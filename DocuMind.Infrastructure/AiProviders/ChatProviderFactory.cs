using System.Runtime.Versioning;
using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;
using DocuMind.Infrastructure.Persistence;
using DocuMind.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Infrastructure.AiProviders;

[SupportedOSPlatform("windows")]
public class ChatProviderFactory : IChatProviderFactory
{
    private readonly AppDbContext _db;
    private readonly HttpClient _httpClient;

    public ChatProviderFactory(AppDbContext db, HttpClient httpClient)
    {
        _db = db;
        _httpClient = httpClient;
    }

    public async Task<IChatCompletionService> GetActiveProviderAsync()
    {
        var activeConfig = await _db.ProviderSettings.FirstOrDefaultAsync(x => x.IsActive)
            ?? throw new InvalidOperationException("Chưa có cấu hình AI Provider nào đang kích hoạt. Vui lòng mở Cài đặt (Settings) để cấu hình API Key.");

        string rawApiKey = WindowsDpapiSecretManager.Decrypt(activeConfig.EncryptedApiKey);

        return activeConfig.ProviderType switch
        {
            AiProviderType.Gemini => new GeminiAiService(_httpClient, rawApiKey, activeConfig.ChatModel),
            AiProviderType.OpenAI => new OpenAiCompatibleService(_httpClient, rawApiKey, activeConfig.BaseUrl, activeConfig.ChatModel, AiProviderType.OpenAI, activeConfig.EmbeddingModel),
            AiProviderType.Groq => new OpenAiCompatibleService(_httpClient, rawApiKey, activeConfig.BaseUrl, activeConfig.ChatModel, AiProviderType.Groq, activeConfig.EmbeddingModel),
            AiProviderType.OllamaLocal => new OpenAiCompatibleService(_httpClient, rawApiKey, activeConfig.BaseUrl, activeConfig.ChatModel, AiProviderType.OllamaLocal, activeConfig.EmbeddingModel),
            _ => throw new NotSupportedException($"AI Provider '{activeConfig.ProviderType}' chưa được hỗ trợ.")
        };
    }

    public async Task SwitchProviderAsync(
        AiProviderType providerType,
        string rawApiKey,
        string modelName,
        string baseUrl = "",
        double temperature = 0.7)
    {
        var allConfigs = await _db.ProviderSettings.ToListAsync();
        foreach (var config in allConfigs)
        {
            config.IsActive = false;
        }

        var target = allConfigs.FirstOrDefault(x => x.ProviderType == providerType);
        if (target == null)
        {
            target = new ProviderSetting
            {
                ProviderType = providerType
            };
            _db.ProviderSettings.Add(target);
        }

        if (!string.IsNullOrWhiteSpace(rawApiKey))
        {
            target.EncryptedApiKey = WindowsDpapiSecretManager.Encrypt(rawApiKey);
        }

        target.ChatModel = modelName;
        target.BaseUrl = baseUrl;
        target.Temperature = temperature;
        target.IsActive = true;

        await _db.SaveChangesAsync();
    }

    public async Task<ProviderSetting?> GetActiveSettingAsync()
    {
        return await _db.ProviderSettings.FirstOrDefaultAsync(x => x.IsActive);
    }

    public async Task<List<ProviderSetting>> GetAllSettingsAsync()
    {
        return await _db.ProviderSettings.ToListAsync();
    }

    public async Task<string> GetDecryptedApiKeyAsync(AiProviderType providerType)
    {
        var setting = await _db.ProviderSettings.FirstOrDefaultAsync(x => x.ProviderType == providerType);
        if (setting == null || string.IsNullOrEmpty(setting.EncryptedApiKey))
            return string.Empty;

        return WindowsDpapiSecretManager.Decrypt(setting.EncryptedApiKey);
    }
}

