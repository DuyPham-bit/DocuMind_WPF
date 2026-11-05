namespace DocuMind.Domain.Entities;

public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; set; }
    public Workspace? Workspace { get; set; }
    public string DisplayName { get; set; } = "";
    public string SourcePath { get; set; } = "";
    public string SourceHash { get; set; } = "";
    public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
}

public enum DocumentVersionStatus { Staging, Ready, Failed }

public class DocumentVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }
    public DocumentVersionStatus Status { get; set; }
    public Guid? EmbeddingProfileId { get; set; }
    public EmbeddingProfile? EmbeddingProfile { get; set; }
    public DateTime IndexedAt { get; set; } = DateTime.UtcNow;
    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
}
