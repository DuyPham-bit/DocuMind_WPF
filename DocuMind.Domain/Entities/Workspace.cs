using DocuMind.Domain.Enums;

namespace DocuMind.Domain.Entities;

public class Workspace
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = "Bạn là trợ lý phân tích tài liệu và nghiên cứu thông tin chuyên nghiệp.";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
