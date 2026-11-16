using DocuMind.Domain.Enums;

namespace DocuMind.Application.Interfaces;

public interface IChatCompletionService
{
    AiProviderType ProviderType { get; }
    bool SupportsChat { get; }
    Task<string> GenerateResponseAsync(string systemPrompt, string userPrompt, double temperature = 0.7, CancellationToken ct = default,
        IReadOnlyList<DocuMind.Application.DTOs.ChatTurn>? history = null);
    Task<bool> TestConnectionAsync(CancellationToken ct = default);
}
