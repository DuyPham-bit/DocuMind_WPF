using System.Net.Http.Json;
using System.Text.Json;
using DocuMind.Application.Interfaces;
using DocuMind.Domain.Enums;

namespace DocuMind.Infrastructure.AiProviders;

public class GeminiAiService : IChatCompletionService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;

    public AiProviderType ProviderType => AiProviderType.Gemini;

    public GeminiAiService(HttpClient http, string apiKey, string model = "gemini-2.5-flash")
    {
        _http = http;
        _apiKey = apiKey;
        _model = string.IsNullOrWhiteSpace(model) ? "gemini-2.5-flash" : model;
    }

    public async Task<string> GenerateResponseAsync(string systemPrompt, string userPrompt, double temperature = 0.7, CancellationToken ct = default)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
        var payload = new
        {
            system_instruction = new { parts = new[] { new { text = systemPrompt } } },
            contents = new[] { new { role = "user", parts = new[] { new { text = userPrompt } } } },
            generationConfig = new { temperature }
        };

        var response = await _http.PostAsJsonAsync(url, payload, ct);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Gemini API Error ({response.StatusCode}): {err}");
        }

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var candidates = doc.RootElement.GetProperty("candidates");
        if (candidates.GetArrayLength() == 0)
            return "Không nhận được phản hồi từ mô hình Gemini.";

        var firstCandidate = candidates[0];
        if (firstCandidate.TryGetProperty("content", out var contentElem) &&
            contentElem.TryGetProperty("parts", out var partsElem) &&
            partsElem.GetArrayLength() > 0)
        {
            return partsElem[0].GetProperty("text").GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/text-embedding-004:embedContent?key={_apiKey}";
        var payload = new
        {
            model = "models/text-embedding-004",
            content = new { parts = new[] { new { text } } }
        };

        var response = await _http.PostAsJsonAsync(url, payload, ct);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Gemini Embedding Error ({response.StatusCode}): {err}");
        }

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var values = doc.RootElement.GetProperty("embedding").GetProperty("values");
        return values.EnumerateArray().Select(x => x.GetSingle()).ToArray();
    }

    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            var testPrompt = "Ping";
            var res = await GenerateResponseAsync("You are a test assistant.", testPrompt, 0.1, ct);
            return !string.IsNullOrWhiteSpace(res);
        }
        catch
        {
            return false;
        }
    }
}

