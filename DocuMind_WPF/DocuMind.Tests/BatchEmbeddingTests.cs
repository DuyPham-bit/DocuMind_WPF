using System.Net;
using System.Text.Json;
using DocuMind.Application.DTOs;
using DocuMind.Application.Interfaces;
using DocuMind.Application.Services;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;
using DocuMind.Infrastructure.AiProviders;
using DocuMind.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Tests;

/// <summary>
/// Nạp tài liệu gửi embedding theo lô thay vì một request cho mỗi đoạn, mà vẫn giữ đúng thứ tự,
/// đúng vector và các bảo vệ cũ (giữ index Ready khi lỗi).
/// </summary>
public class BatchEmbeddingTests
{
    [Fact]
    public async Task GeminiSendsOneBatchEmbedContentsRequestWithEveryTextInOrder()
    {
        var handler = new FakeHandler
        {
            Response = _ => new(HttpStatusCode.OK) { Content = new StringContent("{\"embeddings\":[{\"values\":[1,0]},{\"values\":[0,1]},{\"values\":[1,1]}]}") }
        };
        using var http = new HttpClient(handler);
        IEmbeddingService service = new GeminiAiService(http, "gemini-key", embeddingModel: "gemini-embedding-001");

        var vectors = await service.GenerateEmbeddingsAsync(["một", "hai", "ba"]);

        var request = Assert.Single(handler.Requests);
        Assert.EndsWith("/models/gemini-embedding-001:batchEmbedContents", request.Url.AbsolutePath);
        Assert.Equal("gemini-key", request.Key); // key ở header, không ở URL
        Assert.DoesNotContain("gemini-key", request.Url.AbsoluteUri);
        using var json = JsonDocument.Parse(request.Body);
        var requests = json.RootElement.GetProperty("requests").EnumerateArray().ToList();
        Assert.Equal(["một", "hai", "ba"], requests.Select(r => r.GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString()));
        Assert.All(requests, r => Assert.Equal("models/gemini-embedding-001", r.GetProperty("model").GetString()));
        Assert.Equal([new float[] { 1, 0 }, [0, 1], [1, 1]], vectors);
        Assert.Equal(50, service.MaxBatchSize);
    }

    [Fact]
    public async Task OpenAiCompatibleSendsArrayInputAndReordersByIndex()
    {
        var handler = new FakeHandler
        {
            // Trả về lệch thứ tự: "index" mới là vị trí của input.
            Response = _ => new(HttpStatusCode.OK) { Content = new StringContent("{\"data\":[{\"index\":1,\"embedding\":[0,1]},{\"index\":0,\"embedding\":[1,0]}]}") }
        };
        using var http = new HttpClient(handler);
        IEmbeddingService service = new OpenAiCompatibleService(http, "", "http://localhost:11434/v1", "chat", AiProviderType.OllamaLocal, "bge-m3");

        var vectors = await service.GenerateEmbeddingsAsync(["first", "second"]);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("http://localhost:11434/v1/embeddings", request.Url.AbsoluteUri);
        using var json = JsonDocument.Parse(request.Body);
        Assert.Equal(["first", "second"], json.RootElement.GetProperty("input").EnumerateArray().Select(x => x.GetString()));
        Assert.Equal("bge-m3", json.RootElement.GetProperty("model").GetString());
        Assert.Equal([new float[] { 1, 0 }, [0, 1]], vectors);
    }

    [Fact]
    public async Task OpenAiCompatibleFallsBackToOneRequestPerTextWhenArrayInputIsRejected()
    {
        var handler = new FakeHandler();
        handler.Response = call => call == 1
            ? new(HttpStatusCode.BadRequest) { Content = new StringContent("{\"error\":\"input must be a string\"}") }
            : new(HttpStatusCode.OK) { Content = new StringContent($"{{\"data\":[{{\"embedding\":[{call},0]}}]}}") };
        using var http = new HttpClient(handler);
        IEmbeddingService service = new OpenAiCompatibleService(http, "key", "https://proxy.test/v1", "chat", AiProviderType.OpenAI, "embed");

        var vectors = await service.GenerateEmbeddingsAsync(["a", "b"]);

        Assert.Equal(3, handler.Requests.Count); // 1 request lô bị từ chối + 2 request đơn
        Assert.Equal([new float[] { 2, 0 }, [3, 0]], vectors);
        using var single = JsonDocument.Parse(handler.Requests[1].Body);
        Assert.Equal(JsonValueKind.String, single.RootElement.GetProperty("input").ValueKind);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task AuthErrorsAreNotRetriedAsSingleRequests(HttpStatusCode status)
    {
        var handler = new FakeHandler { Response = _ => new(status) };
        using var http = new HttpClient(handler);
        IEmbeddingService service = new OpenAiCompatibleService(http, "bad-key", "https://api.openai.com/v1", "chat", AiProviderType.OpenAI, "embed");

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => service.GenerateEmbeddingsAsync(["a", "b"]));
        Assert.Equal(status, error.StatusCode);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task BatchWithWrongVectorCountIsRejected()
    {
        var handler = new FakeHandler { Response = _ => new(HttpStatusCode.OK) { Content = new StringContent("{\"data\":[{\"index\":0,\"embedding\":[1,0]}]}") } };
        using var http = new HttpClient(handler);
        IEmbeddingService service = new OpenAiCompatibleService(http, "key", "https://api.openai.com/v1", "chat", AiProviderType.OpenAI, "embed");
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateEmbeddingsAsync(["a", "b"]));
    }

    [Fact]
    public async Task IngestUsesBatchesAndKeepsChunkOrderPagesAndVectors()
    {
        using var fixture = new SqliteFixture();
        var embedder = new BatchingEmbedder(batchSize: 4);
        var providers = new BatchProviders(embedder);
        // 2 trang × 400 từ → mỗi trang 3 đoạn (180 từ, chồng 30) → 6 đoạn → 2 lô (4 + 2).
        var parser = new FakeParser { Pages = [(1, Words("a", 400)), (2, Words("b", 400))] };
        var store = new EfDocumentStore(fixture);
        var rag = new RagOrchestratorService(providers, parser, store);
        var progress = new List<string>();

        await rag.IngestDocumentAsync(SqliteFixture.WorkspaceId, fixture.File(), new SyncProgress(progress.Add));

        Assert.Equal([4, 2], embedder.BatchSizes);
        await using var db = fixture.CreateDbContext();
        var chunks = await db.DocumentChunks.OrderBy(c => c.ChunkIndex).ToListAsync();
        Assert.Equal(Enumerable.Range(0, 6), chunks.Select(c => c.ChunkIndex));
        Assert.Equal([1, 1, 1, 2, 2, 2], chunks.Select(c => c.PageNumber));
        Assert.All(chunks, c => Assert.Equal(JsonSerializer.Serialize(BatchingEmbedder.VectorFor(c.Content)), c.EmbeddingJson));
        Assert.Contains(progress, p => p.Contains("1–4/6"));
        Assert.Contains(progress, p => p.Contains("5–6/6"));
    }

    [Fact]
    public async Task FailedBatchDuringReingestKeepsTheOldReadyIndex()
    {
        using var fixture = new SqliteFixture();
        var embedder = new BatchingEmbedder(batchSize: 4);
        var providers = new BatchProviders(embedder);
        var parser = new FakeParser();
        var rag = new RagOrchestratorService(providers, parser, new EfDocumentStore(fixture));
        var path = fixture.File();
        await rag.IngestDocumentAsync(SqliteFixture.WorkspaceId, path);

        // Lần nạp đầu dùng 1 lô. Nạp lại 900 từ → 6 đoạn → lô 2 (4 đoạn) thành công, lô 3 (2 đoạn) lỗi giữa chừng.
        parser.Pages = [(1, Words("new", 900))];
        embedder.FailOnBatch = 3;
        await Assert.ThrowsAsync<HttpRequestException>(() => rag.IngestDocumentAsync(SqliteFixture.WorkspaceId, path));

        await using var db = fixture.CreateDbContext();
        Assert.Equal([1, 4, 2], embedder.BatchSizes);
        Assert.Equal("old content", Assert.Single(await db.DocumentChunks.Where(c => c.DocumentVersion!.Status == DocumentVersionStatus.Ready).ToListAsync()).Content);
    }

    private static string Words(string word, int count) => string.Join(' ', Enumerable.Range(0, count).Select(i => $"{word}{i}"));

    private sealed class SyncProgress(Action<string> report) : IProgress<string>
    {
        public void Report(string value) => report(value);
    }

    private sealed class BatchingEmbedder(int batchSize) : IEmbeddingService
    {
        public List<int> BatchSizes { get; } = [];
        public int FailOnBatch { get; set; }
        public bool SupportsEmbeddings => true;
        public int MaxBatchSize => batchSize;
        public static float[] VectorFor(string text) => [1, text.Length % 7 + 1];
        public Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default) => Task.FromResult(VectorFor(text));
        public Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken ct = default)
        {
            BatchSizes.Add(texts.Count);
            if (BatchSizes.Count == FailOnBatch) throw new HttpRequestException("fake outage on a later batch");
            return Task.FromResult<IReadOnlyList<float[]>>(texts.Select(VectorFor).ToList());
        }
    }

    private sealed class BatchProviders(IEmbeddingService embedder) : IChatProviderFactory
    {
        private readonly FakeProviders _inner = new();
        public IChatCompletionService CreateTemporary(ProviderConfigDraft draft) => _inner.CreateTemporary(draft);
        public IEmbeddingService CreateTemporaryEmbedding(ProviderConfigDraft draft) => embedder;
        public Task SaveAsync(ProviderConfigDraft draft, CancellationToken ct = default) => Task.CompletedTask;
        public Task<ProviderConfigDraft> LoadDraftAsync(AiProviderType provider, CancellationToken ct = default) => _inner.LoadDraftAsync(provider, ct);
        public Task<ChatSession> GetChatSessionAsync(CancellationToken ct = default) => _inner.GetChatSessionAsync(ct);
        public Task<EmbeddingSession> GetEmbeddingSessionAsync(CancellationToken ct = default) =>
            Task.FromResult(new EmbeddingSession(embedder, AiProviderType.OpenAI, "embedding-a", "https://fake.test/v1"));
        public Task<DocuMind.Domain.Entities.ProviderSetting?> GetActiveSettingAsync() => _inner.GetActiveSettingAsync();
    }
}
