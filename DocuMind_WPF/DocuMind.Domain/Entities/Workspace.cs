using DocuMind.Domain.Enums;

namespace DocuMind.Domain.Entities;

public class Workspace
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = "You are a professional assistant for document analysis and research.";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

    public override string ToString() => Name;
}
