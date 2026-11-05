using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DocuMind.Application.Interfaces;
using DocuMind.Domain.Enums;

namespace DocuMind.Infrastructure.AiProviders;

public class OpenAiCompatibleService : IChatCompletionService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _chatModel;
    private readonly string _embeddingModel;
    private readonly AiProviderType _providerType;

    public AiProviderType ProviderType => _providerType;

    public OpenAiCompatibleService(
        HttpClient http,
        string apiKey,
        string baseUrl,
        string chatModel,
        AiProviderType providerType,
        string embeddingModel = "text-embedding-3-small")
    {
        _http = http;
        _apiKey = apiKey;
        _baseUrl = string.IsNullOrWhiteSpace(baseUrl) ? GetDefaultBaseUrl(providerType) : baseUrl.TrimEnd('/');
        _chatModel = string.IsNullOrWhiteSpace(chatModel) ? GetDefaultChatModel(providerType) : chatModel;
        _providerType = providerType;
        _embeddingModel = string.IsNullOrWhiteSpace(embeddingModel) ? "text-embedding-3-small" : embeddingModel;
    }

    private static string GetDefaultBaseUrl(AiProviderType provider) => provider switch
    {
        AiProviderType.OpenAI => "https://api.openai.com/v1",
        AiProviderType.Groq => "https://api.groq.com/openai/v1",
        AiProviderType.OllamaLocal => "http://localhost:11434/v1",
        _ => "https://api.openai.com/v1"
    };

    private static string GetDefaultChatModel(AiProviderType provider) => provider switch
    {
        AiProviderType.OpenAI => "gpt-4o-mini",
        AiProviderType.Groq => "llama-3.3-70b-versatile",
        AiProviderType.OllamaLocal => "llama3",
        _ => "gpt-4o-mini"
    };

    public async Task<string> GenerateResponseAsync(string systemPrompt, string userPrompt, double temperature = 0.7, CancellationToken ct = default)
    {
        var url = $"{_baseUrl}/chat/completions";
        var payload = new
        {
            model = _chatModel,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = JsonContent.Create(payload);
        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"{_providerType} API Error ({response.StatusCode}): {err}");
        }

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var choices = doc.RootElement.GetProperty("choices");
        if (choices.GetArrayLength() == 0)
            return "Không có nội dung phản hồi từ mô hình.";

        return choices[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var url = $"{_baseUrl}/embeddings";
        var payload = new
        {
            model = _embeddingModel,
            input = text
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = JsonContent.Create(payload);
        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"{_providerType} Embedding Error ({response.StatusCode}): {err}");
        }

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var data = doc.RootElement.GetProperty("data");
        if (data.GetArrayLength() == 0)
            return Array.Empty<float>();

        var embeddingElem = data[0].GetProperty("embedding");
        return embeddingElem.EnumerateArray().Select(x => x.GetSingle()).ToArray();
    }

    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await GenerateResponseAsync("You are a test assistant.", "Ping", 0.1, ct);
            return !string.IsNullOrWhiteSpace(res);
        }
        catch
        {
            return false;
        }
    }
}

