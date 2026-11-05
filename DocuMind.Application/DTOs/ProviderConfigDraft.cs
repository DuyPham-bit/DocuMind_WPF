using DocuMind.Domain.Enums;

namespace DocuMind.Application.DTOs;

public sealed record ProviderConfigDraft(AiProviderType ProviderType, string ApiKey, string ChatModel,
    string EmbeddingModel = "", string BaseUrl = "", double Temperature = 0.7, bool UseForEmbedding = false, bool KeyNeedsReplacement = false)
{
    public override string ToString() => $"ProviderConfigDraft({ProviderType})";
}

public sealed record ChatTurn(string Role, string Content);
public sealed record ChatSession(DocuMind.Application.Interfaces.IChatCompletionService Service,
    AiProviderType Provider, string Model, double Temperature);
public sealed record EmbeddingSession(DocuMind.Application.Interfaces.IEmbeddingService Service,
    AiProviderType Provider, string Model, string Endpoint)
{
    public const string Normalization = "cosine";
    public const string PipelineVersion = "words180-overlap30-v1";
}
