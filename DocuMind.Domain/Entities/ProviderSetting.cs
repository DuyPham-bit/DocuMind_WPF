using DocuMind.Domain.Enums;

namespace DocuMind.Domain.Entities;

public class ProviderSetting
{
    public int Id { get; set; }
    public AiProviderType ProviderType { get; set; }
    public string EncryptedApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string ChatModel { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = string.Empty;
    public double Temperature { get; set; } = 0.7;
    public bool IsActive { get; set; }
}

