using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DocuMind.Application.DTOs;
using DocuMind.Application.Interfaces;
using DocuMind.Domain.Enums;

namespace DocuMind.Infrastructure.AiProviders;

public sealed class OpenAiCompatibleService : IChatCompletionService, IEmbeddingService
{
    private readonly HttpClient _http;
    private readonly string _key, _endpoint, _chatModel, _embeddingModel;
    public AiProviderType ProviderType { get; }
    public bool SupportsChat => true;
    public bool SupportsEmbeddings => ProviderType != AiProviderType.Groq && !string.IsNullOrWhiteSpace(_embeddingModel);

    public OpenAiCompatibleService(HttpClient http, string apiKey, string baseUrl, string chatModel,
        AiProviderType providerType, string embeddingModel = "text-embedding-3-small")
    {
        _http = http; _key = apiKey; _chatModel = chatModel; _embeddingModel = embeddingModel;
        ProviderType = providerType;
        _endpoint = ChatProviderFactory.EffectiveEndpoint(providerType, baseUrl);
    }

    private HttpRequestMessage Request(string route, object payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_endpoint}/{route}") { Content = JsonContent.Create(payload) };
        if (!string.IsNullOrWhiteSpace(_key)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _key);
        return request;
    }

    public async Task<string> GenerateResponseAsync(string systemPrompt, string userPrompt, double temperature = 0.7,
        CancellationToken ct = default, IReadOnlyList<ChatTurn>? history = null)
    {
        var messages = new List<object> { new { role = "system", content = systemPrompt } };
        if (history != null) messages.AddRange(history.Select(t => (object)new { role = t.Role, content = t.Content }));
        messages.Add(new { role = "user", content = userPrompt });
        using var response = await ProviderHttp.SendAsync(_http,
            () => Request("chat/completions", new { model = _chatModel, messages, temperature }), false, ct);
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var choices = doc.RootElement.GetProperty("choices");
        return choices.GetArrayLength() == 0 ? "" : choices[0].GetProperty("message").GetProperty("content").GetString() ?? "";
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        if (!SupportsEmbeddings) throw new InvalidOperationException("Provider chỉ hỗ trợ chat. Hãy chọn provider embedding độc lập.");
        using var response = await ProviderHttp.SendAsync(_http,
            () => Request("embeddings", new { model = _embeddingModel, input = text }), true, ct);
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var data = doc.RootElement.GetProperty("data");
        return data.GetArrayLength() == 0 ? [] : data[0].GetProperty("embedding").EnumerateArray().Select(x => x.GetSingle()).ToArray();
    }

    public async Task<bool> TestConnectionAsync(CancellationToken ct = default) =>
        !string.IsNullOrWhiteSpace(await GenerateResponseAsync("You are a test assistant.", "Ping", 0.1, ct));
}
