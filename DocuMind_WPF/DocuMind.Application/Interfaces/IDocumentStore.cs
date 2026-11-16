using DocuMind.Application.DTOs;
using DocuMind.Domain.Entities;

namespace DocuMind.Application.Interfaces;

/// <summary>Trạng thái index của một tài liệu đối với profile embedding hiện tại, dùng để hiển thị nhãn trên UI.</summary>
public enum DocumentIndexState
{
    /// <summary>Có bản Ready khớp profile embedding hiện tại: RAG/Quiz dùng được.</summary>
    Ready,
    /// <summary>Có bản Ready nhưng của profile embedding khác (đổi model/provider): cần nạp lại.</summary>
    NeedsReindex,
    /// <summary>Không có bản Ready, lần nạp gần nhất bị lỗi hoặc bị hủy.</summary>
    Failed,
    /// <summary>Không có bản Ready, đang có lần nạp chưa xong.</summary>
    Indexing
}

public record DocumentListItem(Guid Id, string DisplayName, string SourcePath, bool NeedsReindex = false, DocumentIndexState? State = null)
{
    /// <summary>Khi store không chỉ rõ trạng thái thì suy ra từ NeedsReindex (tương thích các store/test cũ).</summary>
    public DocumentIndexState IndexState => State ?? (NeedsReindex ? DocumentIndexState.NeedsReindex : DocumentIndexState.Ready);
    public bool CanReindex => IndexState is DocumentIndexState.NeedsReindex or DocumentIndexState.Failed;
    public string Label => NeedsReindex ? DisplayName + " (re-index required)" : DisplayName;
    public override string ToString() => Label;
}

public interface IDocumentStore
{
    Task<DocumentVersion> BeginAsync(Guid workspaceId, string path, CancellationToken ct);
    Task ActivateAsync(Guid versionId, string hash, EmbeddingProfile profile, List<DocumentChunk> chunks, CancellationToken ct);
    Task FailAsync(Guid versionId);
    Task<List<DocumentChunk>> GetCompatibleChunksAsync(Guid workspaceId, EmbeddingSession session, int dimension, CancellationToken ct);
    Task<List<DocumentListItem>> ListAsync(Guid workspaceId, CancellationToken ct = default);
    // NeedsReindex is true unless a Ready version was built with the given embedding profile.
    // Without a profile (no embedding provider selected) only the Ready status is checked.
    Task<List<DocumentListItem>> ListAsync(Guid workspaceId, EmbeddingSession? current, CancellationToken ct = default) => ListAsync(workspaceId, ct);
    Task DeleteAsync(Guid workspaceId, Guid documentId, CancellationToken ct = default);
}
