using DocuMind.Application.DTOs;
using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;
using DocuMind.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Infrastructure.AiProviders;

public sealed class ChatProviderFactory(IDbContextFactory<AppDbContext> contexts, HttpClient http, ISecretProtector secrets) : IChatProviderFactory
{
    public IChatCompletionService CreateTemporary(ProviderConfigDraft draft)
    {
        Validate(draft);
        return draft.ProviderType == AiProviderType.Gemini
            ? new GeminiAiService(http, draft.ApiKey, draft.ChatModel, draft.EmbeddingModel, draft.BaseUrl)
            : new OpenAiCompatibleService(http, draft.ApiKey, draft.BaseUrl, draft.ChatModel, draft.ProviderType, draft.EmbeddingModel);
    }

    public IEmbeddingService CreateTemporaryEmbedding(ProviderConfigDraft draft)
    {
        var service = (IEmbeddingService)CreateTemporary(draft);
        if (!service.SupportsEmbeddings || string.IsNullOrWhiteSpace(draft.EmbeddingModel))
            throw new InvalidOperationException("Provider chỉ hỗ trợ chat hoặc chưa chọn model embedding. Hãy cấu hình embedding độc lập rồi re-index.");
        return service;
    }

    public async Task SaveAsync(ProviderConfigDraft draft, CancellationToken ct = default)
    {
        Validate(draft);
        if (draft.UseForEmbedding) CreateTemporaryEmbedding(draft);
        var encrypted = secrets.Protect(draft.ApiKey); // Fail before tracking or changing any settings.
        await using var db = await contexts.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.ProviderSettings.Where(p => p.IsActive).ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false), ct);
        if (draft.UseForEmbedding)
            await db.ProviderSettings.Where(p => p.IsEmbeddingActive).ExecuteUpdateAsync(s => s.SetProperty(p => p.IsEmbeddingActive, false), ct);
        var settings = await db.ProviderSettings.ToListAsync(ct);
        foreach (var setting in settings)
        {
            setting.IsActive = false;
            if (draft.UseForEmbedding) setting.IsEmbeddingActive = false;
        }
        var target = settings.SingleOrDefault(x => x.ProviderType == draft.ProviderType);
        if (target == null)
        {
            target = new ProviderSetting { ProviderType = draft.ProviderType };
            db.ProviderSettings.Add(target);
        }
        target.EncryptedApiKey = encrypted;
        target.ChatModel = draft.ChatModel.Trim();
        target.EmbeddingModel = draft.EmbeddingModel.Trim();
        target.BaseUrl = EffectiveEndpoint(draft.ProviderType, draft.BaseUrl);
        target.Temperature = draft.Temperature;
        target.IsActive = true;
        target.IsEmbeddingActive = draft.UseForEmbedding;
        await db.SaveChangesAsync(ct); // One atomic EF save for key and active flags.
        await transaction.CommitAsync(ct);
    }

    public async Task<ProviderConfigDraft> LoadDraftAsync(AiProviderType provider, CancellationToken ct = default)
    {
        await using var db = await contexts.CreateDbContextAsync(ct);
        var setting = await db.ProviderSettings.AsNoTracking().SingleOrDefaultAsync(x => x.ProviderType == provider, ct);
        if (setting == null) return Defaults(provider);
        try { return ToDraft(setting); }
        catch (InvalidOperationException)
        {
            return new(setting.ProviderType, "", setting.ChatModel, setting.EmbeddingModel, setting.BaseUrl,
                setting.Temperature, setting.IsEmbeddingActive, KeyNeedsReplacement: true);
        }
    }

    private ProviderConfigDraft ToDraft(ProviderSetting setting) => new(setting.ProviderType,
        secrets.Unprotect(setting.EncryptedApiKey), setting.ChatModel, setting.EmbeddingModel,
        setting.BaseUrl, setting.Temperature, setting.IsEmbeddingActive);

    public async Task<ProviderSetting?> GetActiveSettingAsync()
    {
        await using var db = await contexts.CreateDbContextAsync();
        return await db.ProviderSettings.AsNoTracking().SingleOrDefaultAsync(x => x.IsActive);
    }

    public async Task<ChatSession> GetChatSessionAsync(CancellationToken ct = default)
    {
        var setting = await GetActiveSettingAsync() ?? throw new InvalidOperationException("Hãy mở Cài đặt và lưu provider chat.");
        ct.ThrowIfCancellationRequested();
        return new(CreateTemporary(ToDraft(setting)), setting.ProviderType, setting.ChatModel, setting.Temperature);
    }

    public async Task<EmbeddingSession> GetEmbeddingSessionAsync(CancellationToken ct = default)
    {
        await using var db = await contexts.CreateDbContextAsync(ct);
        var setting = await db.ProviderSettings.AsNoTracking().SingleOrDefaultAsync(x => x.IsEmbeddingActive, ct)
            ?? throw new InvalidOperationException("Chưa chọn provider embedding. Direct Chat vẫn dùng được; hãy chọn embedding trong Cài đặt và re-index để dùng RAG.");
        return new(CreateTemporaryEmbedding(ToDraft(setting)), setting.ProviderType, setting.EmbeddingModel,
            EffectiveEndpoint(setting.ProviderType, setting.BaseUrl));
    }

    public static ProviderConfigDraft Defaults(AiProviderType provider) => provider switch
    {
        AiProviderType.Gemini => new(provider, "", "gemini-2.5-flash", "text-embedding-004"),
        AiProviderType.OpenAI => new(provider, "", "gpt-4o-mini", "text-embedding-3-small"),
        AiProviderType.Groq => new(provider, "", "llama-3.3-70b-versatile"),
        AiProviderType.OllamaLocal => new(provider, "", "llama3"),
        AiProviderType.OpenRouter => new(provider, "", "openrouter/free", "nvidia/nemotron-3-embed-1b:free",
            "https://openrouter.ai/api/v1", Temperature: .3, UseForEmbedding: true),
        _ => throw new NotSupportedException("Provider không được hỗ trợ.")
    };

    public static string EffectiveEndpoint(AiProviderType provider, string url) => string.IsNullOrWhiteSpace(url) ? provider switch
    {
        AiProviderType.Gemini => "https://generativelanguage.googleapis.com/v1beta",
        AiProviderType.OpenAI => "https://api.openai.com/v1",
        AiProviderType.Groq => "https://api.groq.com/openai/v1",
        AiProviderType.OllamaLocal => "http://localhost:11434/v1",
        AiProviderType.OpenRouter => "https://openrouter.ai/api/v1",
        _ => throw new NotSupportedException("Provider không được hỗ trợ.")
    } : url.Trim().TrimEnd('/');

    private static void Validate(ProviderConfigDraft draft)
    {
        if (!Enum.IsDefined(draft.ProviderType) || string.IsNullOrWhiteSpace(draft.ChatModel))
            throw new InvalidOperationException("Hãy chọn provider và model chat.");
        if (draft.ProviderType != AiProviderType.OllamaLocal && string.IsNullOrWhiteSpace(draft.ApiKey))
            throw new InvalidOperationException("Hãy nhập API key.");
        if (!double.IsFinite(draft.Temperature) || draft.Temperature is < 0 or > 2)
            throw new InvalidOperationException("Temperature phải nằm trong khoảng 0 đến 2.");
        var endpoint = EffectiveEndpoint(draft.ProviderType, draft.BaseUrl);
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "https" && !(uri.Scheme == "http" && uri.IsLoopback)) ||
            uri.Query.Length != 0 || uri.UserInfo.Length != 0 || uri.Fragment.Length != 0)
            throw new InvalidOperationException("Base URL phải là HTTPS (hoặc HTTP localhost), không chứa key, query hay thông tin đăng nhập.");
    }
}
