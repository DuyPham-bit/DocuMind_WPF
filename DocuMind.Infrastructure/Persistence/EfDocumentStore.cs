using DocuMind.Application.DTOs;
using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Infrastructure.Persistence;

public sealed class EfDocumentStore(IDbContextFactory<AppDbContext> contexts) : IDocumentStore
{
    public async Task<DocumentVersion> BeginAsync(Guid workspaceId, string path, CancellationToken ct)
    {
        await using var db = await contexts.CreateDbContextAsync(ct);
        var fullPath = Path.GetFullPath(path);
        var name = Path.GetFileName(path);
        var document = await db.Documents.SingleOrDefaultAsync(d => d.WorkspaceId == workspaceId && d.SourcePath == fullPath, ct);
        document ??= await db.Documents.SingleOrDefaultAsync(d => d.WorkspaceId == workspaceId && d.SourcePath == "legacy:" + name, ct);
        if (document == null)
        {
            document = new Document { WorkspaceId = workspaceId, SourcePath = fullPath, DisplayName = name };
            db.Documents.Add(document);
        }
        else document.SourcePath = fullPath;
        var version = new DocumentVersion { DocumentId = document.Id, Status = DocumentVersionStatus.Staging };
        db.DocumentVersions.Add(version);
        await db.SaveChangesAsync(ct);
        return version;
    }

    public async Task ActivateAsync(Guid versionId, string hash, EmbeddingProfile profile, List<DocumentChunk> chunks, CancellationToken ct)
    {
        if (chunks.Count == 0) throw new InvalidOperationException("Tài liệu không có đoạn văn bản hợp lệ.");
        await using var db = await contexts.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var version = await db.DocumentVersions.Include(v => v.Document).SingleAsync(v => v.Id == versionId, ct);
        if (version.Status != DocumentVersionStatus.Staging) throw new InvalidOperationException("Phiên index đã kết thúc.");
        var existingProfile = await db.EmbeddingProfiles.SingleOrDefaultAsync(p =>
            p.Provider == profile.Provider && p.Model == profile.Model && p.Endpoint == profile.Endpoint &&
            p.Dimension == profile.Dimension && p.Normalization == profile.Normalization && p.PipelineVersion == profile.PipelineVersion, ct);
        if (existingProfile == null) db.EmbeddingProfiles.Add(profile);
        version.EmbeddingProfileId = (existingProfile ?? profile).Id;
        // Short transaction: all HTTP/parse/serialization already completed. Removing old versions is rollback-safe.
        await db.DocumentVersions.Where(v => v.DocumentId == version.DocumentId && v.Id != versionId && v.Status != DocumentVersionStatus.Staging)
            .ExecuteDeleteAsync(ct);
        foreach (var chunk in chunks)
        {
            chunk.DocumentVersionId = versionId;
            chunk.WorkspaceId = version.Document!.WorkspaceId;
            chunk.FileName = version.Document.DisplayName;
        }
        db.DocumentChunks.AddRange(chunks);
        version.Status = DocumentVersionStatus.Ready;
        version.IndexedAt = DateTime.UtcNow;
        version.Document!.SourceHash = hash;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task FailAsync(Guid versionId)
    {
        await using var db = await contexts.CreateDbContextAsync();
        await db.DocumentVersions.Where(v => v.Id == versionId && v.Status == DocumentVersionStatus.Staging)
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.Status, DocumentVersionStatus.Failed));
    }

    public async Task<List<DocumentChunk>> GetCompatibleChunksAsync(Guid workspaceId, EmbeddingSession session, int dimension, CancellationToken ct)
    {
        await using var db = await contexts.CreateDbContextAsync(ct);
        return await db.DocumentChunks.AsNoTracking().Include(c => c.DocumentVersion!).ThenInclude(v => v.Document)
            .Where(c => c.WorkspaceId == workspaceId && c.DocumentVersion != null && c.DocumentVersion.Status == DocumentVersionStatus.Ready &&
                c.DocumentVersion.EmbeddingProfile != null && c.DocumentVersion.EmbeddingProfile.Provider == session.Provider &&
                c.DocumentVersion.EmbeddingProfile.Model == session.Model && c.DocumentVersion.EmbeddingProfile.Endpoint == session.Endpoint &&
                c.DocumentVersion.EmbeddingProfile.Dimension == dimension && c.DocumentVersion.EmbeddingProfile.Normalization == EmbeddingSession.Normalization &&
                c.DocumentVersion.EmbeddingProfile.PipelineVersion == EmbeddingSession.PipelineVersion)
            .OrderBy(c => c.DocumentVersionId).ThenBy(c => c.ChunkIndex).ToListAsync(ct);
    }

    public async Task<List<DocumentListItem>> ListAsync(Guid workspaceId, CancellationToken ct = default)
    {
        await using var db = await contexts.CreateDbContextAsync(ct);
        return await db.Documents.AsNoTracking().Where(d => d.WorkspaceId == workspaceId).OrderBy(d => d.DisplayName)
            .Select(d => new DocumentListItem(d.Id, d.DisplayName, d.SourcePath, !d.Versions.Any(v => v.Status == DocumentVersionStatus.Ready))).ToListAsync(ct);
    }

    public async Task DeleteAsync(Guid workspaceId, Guid documentId, CancellationToken ct = default)
    {
        await using var db = await contexts.CreateDbContextAsync(ct);
        await db.Documents.Where(d => d.WorkspaceId == workspaceId && d.Id == documentId).ExecuteDeleteAsync(ct);
    }
}
