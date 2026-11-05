using DocuMind.Application.DTOs;
using DocuMind.Domain.Enums;

namespace DocuMind.WPF.Models;

public class ChatMessageDisplayModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public MessageRole Role { get; set; }
    public string SenderName => Role == MessageRole.User ? "Bạn" : "DocuMind AI";
    public bool IsUser => Role == MessageRole.User;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string FormattedTime => Timestamp.ToString("HH:mm");
    public List<RagSourceDto> Sources { get; set; } = new();
    public bool HasSources => Sources != null && Sources.Count > 0;
}

