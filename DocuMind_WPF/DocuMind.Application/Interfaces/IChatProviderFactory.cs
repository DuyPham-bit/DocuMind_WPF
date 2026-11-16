using DocuMind.Application.DTOs;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;

namespace DocuMind.Application.Interfaces;

public interface IChatProviderFactory
{
    IChatCompletionService CreateTemporary(ProviderConfigDraft draft);
    IEmbeddingService CreateTemporaryEmbedding(ProviderConfigDraft draft);
    Task SaveAsync(ProviderConfigDraft draft, CancellationToken ct = default);
    Task<ProviderConfigDraft> LoadDraftAsync(AiProviderType provider, CancellationToken ct = default);
    Task<ChatSession> GetChatSessionAsync(CancellationToken ct = default);
    Task<EmbeddingSession> GetEmbeddingSessionAsync(CancellationToken ct = default);
    Task<ProviderSetting?> GetActiveSettingAsync();
}
