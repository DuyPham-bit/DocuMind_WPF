using DocuMind.Application.DTOs;
using DocuMind.Domain.Enums;

namespace DocuMind.WPF.Models;

public class ChatMessageDisplayModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public MessageRole Role { get; set; }
    public ChatMode Mode { get; set; }
    public string Provider { get; set; } = "";
    public string Model { get; set; } = "";
    public double Temperature { get; set; }
    public string SenderName => Role == MessageRole.User ? "Bạn" : "DocuMind AI";
    public bool IsUser => Role == MessageRole.User;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string FormattedTime => Timestamp.ToString("HH:mm");
    public List<RagSourceDto> Sources { get; set; } = new();
    public bool HasSources => Sources != null && Sources.Count > 0;

    public static ChatMessageDisplayModel FromEntity(DocuMind.Domain.Entities.ChatMessage message)
    {
        List<RagSourceDto> sources = [];
        if (!string.IsNullOrWhiteSpace(message.SourcesJson))
        {
            try { sources = System.Text.Json.JsonSerializer.Deserialize<List<RagSourceDto>>(message.SourcesJson) ?? []; }
            catch (System.Text.Json.JsonException ex) { DocuMind.Application.Services.UserErrors.Describe(ex); }
        }
        return new() { Id = message.Id, Role = message.Role, Content = message.Content, Timestamp = message.Timestamp.ToLocalTime(),
            Mode = message.Mode, Provider = message.Provider, Model = message.Model, Temperature = message.Temperature, Sources = sources };
    }
}
