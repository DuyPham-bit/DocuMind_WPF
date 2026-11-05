namespace DocuMind.Domain.Entities;

public class DocumentChunk
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; set; }
    public Workspace? Workspace { get; set; }
    // Null only for legacy indexes: preserved during migration, excluded from retrieval until re-index.
    public Guid? DocumentVersionId { get; set; }
    public DocumentVersion? DocumentVersion { get; set; }
    public int ChunkIndex { get; set; }

    public string FileName { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public string EmbeddingJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
