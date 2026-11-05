using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;

namespace DocuMind.Application.Interfaces;

public interface IChatProviderFactory
{
    Task<IChatCompletionService> GetActiveProviderAsync();
    Task SwitchProviderAsync(AiProviderType providerType, string rawApiKey, string modelName, string baseUrl = "", double temperature = 0.7);
    Task<ProviderSetting?> GetActiveSettingAsync();
    Task<List<ProviderSetting>> GetAllSettingsAsync();
    Task<string> GetDecryptedApiKeyAsync(AiProviderType providerType);
}

