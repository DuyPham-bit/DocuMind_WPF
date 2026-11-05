using System.Net.Http.Json;
using System.Text.Json;
using DocuMind.Application.DTOs;
using DocuMind.Application.Interfaces;
using DocuMind.Domain.Enums;

namespace DocuMind.Infrastructure.AiProviders;

public sealed class GeminiAiService : IChatCompletionService, IEmbeddingService
{
    private readonly HttpClient _http;
    private readonly string _key, _model, _embeddingModel, _endpoint;
    public AiProviderType ProviderType => AiProviderType.Gemini;
    public bool SupportsChat => true;
    public bool SupportsEmbeddings => !string.IsNullOrWhiteSpace(_embeddingModel);

    public GeminiAiService(HttpClient http, string apiKey, string model = "gemini-2.5-flash",
        string embeddingModel = "text-embedding-004", string baseUrl = "")
    {
        _http = http; _key = apiKey; _model = model; _embeddingModel = embeddingModel;
        _endpoint = ChatProviderFactory.EffectiveEndpoint(AiProviderType.Gemini, baseUrl);
    }

    private HttpRequestMessage Request(string model, string action, object payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_endpoint}/models/{Uri.EscapeDataString(model)}:{action}")
            { Content = JsonContent.Create(payload) };
        request.Headers.Add("x-goog-api-key", _key); // Never put secrets into URLs or exception messages.
        return request;
    }

    public async Task<string> GenerateResponseAsync(string systemPrompt, string userPrompt, double temperature = 0.7,
        CancellationToken ct = default, IReadOnlyList<ChatTurn>? history = null)
    {
        var contents = new List<object>();
        if (history != null) contents.AddRange(history.Select(t => (object)new
            { role = t.Role == "assistant" ? "model" : "user", parts = new[] { new { text = t.Content } } }));
        contents.Add(new { role = "user", parts = new[] { new { text = userPrompt } } });
        var payload = new { system_instruction = new { parts = new[] { new { text = systemPrompt } } }, contents,
            generationConfig = new { temperature } };
        using var response = await ProviderHttp.SendAsync(_http, () => Request(_model, "generateContent", payload), false, ct);
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var candidates = doc.RootElement.GetProperty("candidates");
        if (candidates.GetArrayLength() == 0 || !candidates[0].TryGetProperty("content", out var content)) return "";
        return string.Concat(content.GetProperty("parts").EnumerateArray()
            .Where(x => x.TryGetProperty("text", out _)).Select(x => x.GetProperty("text").GetString()));
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        if (!SupportsEmbeddings) throw new InvalidOperationException("Chưa chọn model embedding.");
        var payload = new { model = $"models/{_embeddingModel}", content = new { parts = new[] { new { text } } } };
        using var response = await ProviderHttp.SendAsync(_http, () => Request(_embeddingModel, "embedContent", payload), true, ct);
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return doc.RootElement.GetProperty("embedding").GetProperty("values").EnumerateArray().Select(x => x.GetSingle()).ToArray();
    }

    public async Task<bool> TestConnectionAsync(CancellationToken ct = default) =>
        !string.IsNullOrWhiteSpace(await GenerateResponseAsync("You are a test assistant.", "Ping", 0.1, ct));
}
