using DocuMind.Domain.Enums;

namespace DocuMind.Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; set; }
    public Workspace? Workspace { get; set; }

    public MessageRole Role { get; set; } = MessageRole.User;
    public string Content { get; set; } = string.Empty;
    public string? SourcesJson { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

