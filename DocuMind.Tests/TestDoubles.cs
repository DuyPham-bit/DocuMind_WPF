using System.Net;
using DocuMind.Application.DTOs;
using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;
using DocuMind.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Tests;

public sealed class SqliteFixture : IDbContextFactory<AppDbContext>, IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "DocuMindTests-" + Guid.NewGuid().ToString("N"));
    public string DbPath => Path.Combine(Root, "sample.db");
    public static Guid WorkspaceId => Guid.Parse("11111111-1111-1111-1111-111111111111");
    public int ContextCount;
    public SqliteFixture(bool migrate = true)
    {
        Directory.CreateDirectory(Root);
        if (migrate) { using var db = CreateDbContext(); db.Database.Migrate(); }
    }
    public AppDbContext CreateDbContext()
    {
        Interlocked.Increment(ref ContextCount);
        return new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(DatabaseMaintenance.ConnectionString(DbPath)).Options);
    }
    public Task<AppDbContext> CreateDbContextAsync(CancellationToken ct = default) { ct.ThrowIfCancellationRequested(); return Task.FromResult(CreateDbContext()); }
    public string File(string name = "sample.txt", string content = "old content")
    {
        var path = Path.Combine(Root, name); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        System.IO.File.WriteAllText(path, content); return path;
    }
    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        var full = Path.GetFullPath(Root);
        if (Path.GetDirectoryName(full) != Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) ||
            !Path.GetFileName(full).StartsWith("DocuMindTests-")) throw new InvalidOperationException("Unsafe fixture cleanup path.");
        Directory.Delete(full, true);
    }
}

public sealed class FakeAi : IChatCompletionService, IEmbeddingService
{
    public AiProviderType ProviderType => AiProviderType.OpenAI;
    public bool SupportsChat => true;
    public bool SupportsEmbeddings => true;
    public Func<string, CancellationToken, Task<float[]>> Embed { get; set; } = (_, _) => Task.FromResult(new float[] { 1, 0 });
    public Func<CancellationToken, Task<string>> Respond { get; set; } = _ => Task.FromResult("answer");
    public List<(string System, string User, double Temperature)> Requests { get; } = [];
    public Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default) => Embed(text, ct);
    public Task<string> GenerateResponseAsync(string systemPrompt, string userPrompt, double temperature = .7, CancellationToken ct = default, IReadOnlyList<ChatTurn>? history = null)
    {
        Requests.Add((systemPrompt, userPrompt, temperature)); return Respond(ct);
    }
    public async Task<bool> TestConnectionAsync(CancellationToken ct = default) => !string.IsNullOrWhiteSpace(await Respond(ct));
}

public sealed class FakeProviders : IChatProviderFactory
{
    public FakeAi Ai { get; } = new();
    public string EmbeddingModel = "embedding-a";
    public string Endpoint = "https://fake.test/v1";
    public double Temperature = .23;
    public bool EmbeddingEnabled = true;
    public int Saves;
    public Func<AiProviderType, Task<ProviderConfigDraft>> Load { get; set; } = p => Task.FromResult(new ProviderConfigDraft(p, "", "model"));
    public IChatCompletionService CreateTemporary(ProviderConfigDraft draft) => Ai;
    public IEmbeddingService CreateTemporaryEmbedding(ProviderConfigDraft draft) => Ai;
    public Task SaveAsync(ProviderConfigDraft draft, CancellationToken ct = default) { Saves++; return Task.CompletedTask; }
    public Task<ProviderConfigDraft> LoadDraftAsync(AiProviderType provider, CancellationToken ct = default) => Load(provider);
    public Task<ChatSession> GetChatSessionAsync(CancellationToken ct = default) => Task.FromResult(new ChatSession(Ai, AiProviderType.OpenAI, "chat", Temperature));
    public Task<EmbeddingSession> GetEmbeddingSessionAsync(CancellationToken ct = default) => EmbeddingEnabled
        ? Task.FromResult(new EmbeddingSession(Ai, AiProviderType.OpenAI, EmbeddingModel, Endpoint))
        : throw new InvalidOperationException("Select embedding provider; Direct Chat is available.");
    public Task<ProviderSetting?> GetActiveSettingAsync() => Task.FromResult<ProviderSetting?>(null);
}

public sealed class FakeParser : IDocumentParser
{
    public List<(int PageNumber, string Text)> Pages { get; set; } = [(1, "old content")];
    public Exception? Error;
    public Task<List<(int PageNumber, string Text)>> ParseFileAsync(string filePath, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested(); if (Error != null) throw Error; return Task.FromResult(Pages);
    }
}

public sealed class FakeSecrets : ISecretProtector
{
    public bool FailProtect;
    public string Protect(string plainText) => FailProtect ? throw new InvalidOperationException("protect failure") : "protected:" + plainText;
    public string Unprotect(string cipherText) => cipherText.StartsWith("protected:") ? cipherText[10..] : "";
}

public sealed class FakeHandler : HttpMessageHandler
{
    public List<(Uri Url, string Body, string? Key)> Requests { get; } = [];
    public Func<int, HttpResponseMessage> Response { get; set; } = _ => new(HttpStatusCode.OK) { Content = new StringContent("{\"choices\":[{\"message\":{\"content\":\"ok\"}}]}") };
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Requests.Add((request.RequestUri!, request.Content == null ? "" : await request.Content.ReadAsStringAsync(ct),
            request.Headers.Authorization?.Parameter ?? (request.Headers.TryGetValues("x-goog-api-key", out var values) ? values.Single() : null)));
        return Response(Requests.Count);
    }
}
