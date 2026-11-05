using DocuMind.Application.DTOs;
using DocuMind.Domain.Entities;

namespace DocuMind.Application.Interfaces;

public record DocumentListItem(Guid Id, string DisplayName, string SourcePath, bool NeedsReindex = false)
{
    public string Label => NeedsReindex ? DisplayName + " (cần re-index)" : DisplayName;
    public override string ToString() => Label;
}

public interface IDocumentStore
{
    Task<DocumentVersion> BeginAsync(Guid workspaceId, string path, CancellationToken ct);
    Task ActivateAsync(Guid versionId, string hash, EmbeddingProfile profile, List<DocumentChunk> chunks, CancellationToken ct);
    Task FailAsync(Guid versionId);
    Task<List<DocumentChunk>> GetCompatibleChunksAsync(Guid workspaceId, EmbeddingSession session, int dimension, CancellationToken ct);
    Task<List<DocumentListItem>> ListAsync(Guid workspaceId, CancellationToken ct = default);
    Task DeleteAsync(Guid workspaceId, Guid documentId, CancellationToken ct = default);
}
