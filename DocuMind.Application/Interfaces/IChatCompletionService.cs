using DocuMind.Domain.Enums;

namespace DocuMind.Application.Interfaces;

public interface IChatCompletionService
{
    AiProviderType ProviderType { get; }
    Task<string> GenerateResponseAsync(string systemPrompt, string userPrompt, double temperature = 0.7, CancellationToken ct = default);
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default);
    Task<bool> TestConnectionAsync(CancellationToken ct = default);
}

