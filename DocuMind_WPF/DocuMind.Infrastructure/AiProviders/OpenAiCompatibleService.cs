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

    private HttpRequestMessage Request(string route, object payload, string? endpoint = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint ?? _endpoint}/{route}") { Content = JsonContent.Create(payload) };
        if (!string.IsNullOrWhiteSpace(_key)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _key);
        return request;
    }

    public async Task<string> GenerateResponseAsync(string systemPrompt, string userPrompt, double temperature = 0.7,
        CancellationToken ct = default, IReadOnlyList<ChatTurn>? history = null)
    {
        var messages = new List<object> { new { role = "system", content = systemPrompt } };
        if (history != null) messages.AddRange(history.Select(t => (object)new { role = t.Role, content = t.Content }));
        messages.Add(new { role = "user", content = userPrompt });
        if (ProviderType == AiProviderType.OllamaLocal)
        {
            // The OpenAI-compatible route cannot set num_ctx. Use Ollama's native chat
            // route so every request gets the larger context without restarting its server.
            var endpoint = _endpoint.EndsWith("/v1", StringComparison.OrdinalIgnoreCase) ? _endpoint[..^3] : _endpoint;
            using var localResponse = await ProviderHttp.SendAsync(_http,
                () => Request("api/chat", new
                {
                    model = _chatModel, messages, stream = false,
                    options = new { num_ctx = 8192, num_predict = 3072, temperature }
                }, endpoint), false, ct);
            using var localDoc = await JsonDocument.ParseAsync(await localResponse.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            return localDoc.RootElement.GetProperty("message").GetProperty("content").GetString() ?? "";
        }
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

    /// <summary>OpenAI, OpenRouter và Ollama (/v1/embeddings) đều nhận "input" là mảng chuỗi.</summary>
    public int MaxBatchSize => 32;

    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        if (!SupportsEmbeddings) throw new InvalidOperationException("Provider chỉ hỗ trợ chat. Hãy chọn provider embedding độc lập.");
        if (texts.Count == 0) return [];
        if (texts.Count == 1) return [await GenerateEmbeddingAsync(texts[0], ct)];
        HttpResponseMessage response;
        try
        {
            response = await ProviderHttp.SendAsync(_http,
                () => Request("embeddings", new { model = _embeddingModel, input = texts.ToArray() }), true, ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.NotFound
                                               or System.Net.HttpStatusCode.RequestEntityTooLarge or System.Net.HttpStatusCode.UnprocessableEntity)
        {
            // Một số endpoint "tương thích OpenAI" không nhận input dạng mảng: quay về gọi từng đoạn như trước.
            var single = new List<float[]>(texts.Count);
            foreach (var text in texts) single.Add(await GenerateEmbeddingAsync(text, ct));
            return single;
        }
        using (response)
        {
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var items = doc.RootElement.GetProperty("data").EnumerateArray().Select((item, position) => (
                Index: item.TryGetProperty("index", out var index) && index.TryGetInt32(out var i) ? i : position,
                Vector: item.GetProperty("embedding").EnumerateArray().Select(x => x.GetSingle()).ToArray())).ToList();
            if (items.Count != texts.Count || items.Select(x => x.Index).Distinct().Count() != texts.Count || items.Any(x => x.Index < 0 || x.Index >= texts.Count))
                throw new InvalidOperationException("The embedding batch returned a different number of vectors. The previous index was preserved.");
            return items.OrderBy(x => x.Index).Select(x => x.Vector).ToList(); // "index" chỉ vị trí của input, không bảo đảm thứ tự trả về.
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken ct = default) =>
        !string.IsNullOrWhiteSpace(await GenerateResponseAsync("You are a test assistant.", "Ping", 0.1, ct));
}
