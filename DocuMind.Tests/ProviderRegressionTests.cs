using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DocuMind.Application.DTOs;
using DocuMind.Domain.Enums;
using DocuMind.Infrastructure.AiProviders;
using DocuMind.Infrastructure.Persistence;
using DocuMind.Infrastructure.Security;
using DocuMind.WPF.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Tests;

public class ProviderRegressionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OpenRouterCanBeConfiguredByPastingOnlyAKeyWithoutChangingSavedSettingsUntilSave(bool hasExistingProvider)
    {
        using var fixture = new SqliteFixture();
        var handler = new FakeHandler();
        handler.Response = _ => new(HttpStatusCode.OK)
        {
            Content = new StringContent(handler.Requests.Last().Url.AbsolutePath.EndsWith("/embeddings")
                ? "{\"data\":[{\"embedding\":[1,0]}]}"
                : "{\"choices\":[{\"message\":{\"content\":\"ok\"}}]}")
        };
        using var http = new HttpClient(handler);
        var factory = new ChatProviderFactory(fixture, http, new FakeSecrets());
        if (hasExistingProvider)
            await factory.SaveAsync(new(AiProviderType.Gemini, "previous-key", "previous-chat", "previous-embedding", UseForEmbedding: true));

        using var viewModel = new SettingsViewModel(factory);
        await viewModel.InitializeAsync();
        if (hasExistingProvider) Assert.Equal(AiProviderType.Gemini, viewModel.SelectedProvider);
        await viewModel.UseOpenRouterCommand.ExecuteAsync(null);
        Assert.Contains(AiProviderType.OpenRouter, viewModel.AvailableProviders);
        Assert.Equal(AiProviderType.OpenRouter, viewModel.SelectedProvider);
        Assert.Empty(viewModel.ApiKey);
        Assert.Equal("openrouter/free", viewModel.ChatModel);
        Assert.Equal("nvidia/nemotron-3-embed-1b:free", viewModel.EmbeddingModel);
        Assert.Equal("https://openrouter.ai/api/v1", viewModel.BaseUrl);
        Assert.Equal(.3, viewModel.Temperature);
        Assert.True(viewModel.UseForEmbedding);

        viewModel.ApiKey = "test-openrouter-key";
        // Selecting the shortcut again must keep a key the user has just pasted.
        await viewModel.UseOpenRouterCommand.ExecuteAsync(null);
        Assert.Equal("test-openrouter-key", viewModel.ApiKey);
        await viewModel.TestConnectionCommand.ExecuteAsync(null);
        Assert.True(viewModel.IsSuccessStatus);
        await viewModel.TestEmbeddingCommand.ExecuteAsync(null);
        Assert.True(viewModel.IsSuccessStatus);
        var previous = await factory.GetActiveSettingAsync();
        if (hasExistingProvider) Assert.Equal(AiProviderType.Gemini, previous!.ProviderType);
        else Assert.Null(previous);

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("https://openrouter.ai/api/v1/chat/completions", handler.Requests[0].Url.AbsoluteUri);
        Assert.Equal("https://openrouter.ai/api/v1/embeddings", handler.Requests[1].Url.AbsoluteUri);
        Assert.All(handler.Requests, request => Assert.Equal("test-openrouter-key", request.Key));
        using var chat = JsonDocument.Parse(handler.Requests[0].Body);
        using var embedding = JsonDocument.Parse(handler.Requests[1].Body);
        Assert.Equal(viewModel.ChatModel, chat.RootElement.GetProperty("model").GetString());
        Assert.Equal(viewModel.EmbeddingModel, embedding.RootElement.GetProperty("model").GetString());

        await viewModel.SaveCommand.ExecuteAsync(null);
        Assert.True(viewModel.IsSuccessStatus);
        Assert.Equal(AiProviderType.OpenRouter, (await factory.GetChatSessionAsync()).Provider);
        Assert.Equal(AiProviderType.OpenRouter, (await factory.GetEmbeddingSessionAsync()).Provider);
        Assert.Equal(viewModel.CaptureDraft(), await factory.LoadDraftAsync(AiProviderType.OpenRouter));
    }

    [Theory]
    [InlineData("pass")]
    [InlineData("fail")]
    [InlineData("cancel")]
    public async Task ConnectionTestDoesNotWriteOrSwitchDatabase(string outcome)
    {
        using var fixture = new SqliteFixture(); var handler = new FakeHandler();
        using var http = new HttpClient(handler); var factory = new ChatProviderFactory(fixture, http, new FakeSecrets());
        var original = new ProviderConfigDraft(AiProviderType.OpenAI, "key-a", "chat-a", "embed-a", Temperature: .17, UseForEmbedding: true);
        await factory.SaveAsync(original);
        original = await factory.LoadDraftAsync(AiProviderType.OpenAI);
        var draft = original with { ProviderType = AiProviderType.Groq, ApiKey = "key-b", ChatModel = "chat-b", EmbeddingModel = "", UseForEmbedding = false };
        handler.Response = _ => outcome == "fail" ? new(HttpStatusCode.Unauthorized) { Content = new StringContent("key-b should never be echoed") }
            : new(HttpStatusCode.OK) { Content = new StringContent("{\"choices\":[{\"message\":{\"content\":\"ok\"}}]}") };
        using var cancel = new CancellationTokenSource(); if (outcome == "cancel") cancel.Cancel();
        var temporary = factory.CreateTemporary(draft);
        if (outcome == "pass") Assert.True(await temporary.TestConnectionAsync(cancel.Token));
        else if (outcome == "cancel") await Assert.ThrowsAnyAsync<OperationCanceledException>(() => temporary.TestConnectionAsync(cancel.Token));
        else
        {
            var error = await Assert.ThrowsAsync<HttpRequestException>(() => temporary.TestConnectionAsync(cancel.Token));
            Assert.DoesNotContain("key-b", error.Message);
        }
        Assert.Equal(original, await factory.LoadDraftAsync(AiProviderType.OpenAI));
        Assert.Equal(AiProviderType.OpenAI, (await factory.GetChatSessionAsync()).Provider);
        await using var db = fixture.CreateDbContext(); Assert.Equal(1, await db.ProviderSettings.CountAsync());
    }

    [Fact]
    public async Task ProtectFailureDoesNotCreateKeyOrDeactivatePreviousProvider()
    {
        using var fixture = new SqliteFixture(); using var http = new HttpClient(new FakeHandler());
        var secrets = new FakeSecrets(); var factory = new ChatProviderFactory(fixture, http, secrets);
        var original = new ProviderConfigDraft(AiProviderType.OpenAI, "key-a", "chat", "embed", UseForEmbedding: true);
        await factory.SaveAsync(original); original = await factory.LoadDraftAsync(AiProviderType.OpenAI); secrets.FailProtect = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => factory.SaveAsync(new(AiProviderType.Groq, "key-b", "chat-b")));
        Assert.Equal(original, await factory.LoadDraftAsync(AiProviderType.OpenAI));
        await using var db = fixture.CreateDbContext(); Assert.Equal(1, await db.ProviderSettings.CountAsync());
    }

    [Fact]
    public async Task DatabaseFailureDuringProviderSaveRollsBackKeyAndActiveFlags()
    {
        using var fixture = new SqliteFixture(); using var http = new HttpClient(new FakeHandler());
        var factory = new ChatProviderFactory(fixture, http, new FakeSecrets());
        await factory.SaveAsync(new(AiProviderType.OpenAI, "key-a", "chat-a", "embed-a", UseForEmbedding: true));
        var before = await factory.LoadDraftAsync(AiProviderType.OpenAI);
        await using (var db = fixture.CreateDbContext())
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailProviderInsert BEFORE INSERT ON ProviderSettings BEGIN SELECT RAISE(ABORT, 'simulated write failure'); END;");
        await Assert.ThrowsAsync<DbUpdateException>(() => factory.SaveAsync(new(AiProviderType.Gemini, "key-b", "chat-b", "embed-b", UseForEmbedding: true)));
        Assert.Equal(before, await factory.LoadDraftAsync(AiProviderType.OpenAI));
        Assert.Equal(AiProviderType.OpenAI, (await factory.GetChatSessionAsync()).Provider);
        Assert.Equal(AiProviderType.OpenAI, (await factory.GetEmbeddingSessionAsync()).Provider);
    }

    [Fact]
    public async Task ChatSwitchKeepsIndependentEmbeddingProviderAndItsKey()
    {
        using var fixture = new SqliteFixture(); var handler = new FakeHandler(); using var http = new HttpClient(handler);
        var factory = new ChatProviderFactory(fixture, http, new FakeSecrets());
        await factory.SaveAsync(new(AiProviderType.OpenAI, "key-a", "chat-a", "embedding-a", UseForEmbedding: true));
        await factory.SaveAsync(new(AiProviderType.Groq, "key-b", "chat-b", Temperature: .19));
        var embedding = await factory.GetEmbeddingSessionAsync(); Assert.Equal(AiProviderType.OpenAI, embedding.Provider);
        await (await factory.GetChatSessionAsync()).Service.GenerateResponseAsync("system", "user", .19);
        Assert.Equal("key-b", Assert.Single(handler.Requests).Key);
        handler.Response = _ => new(HttpStatusCode.OK) { Content = new StringContent("{\"data\":[{\"embedding\":[1,0]}]}") };
        await embedding.Service.GenerateEmbeddingAsync("text");
        Assert.Equal("key-a", handler.Requests.Last().Key); Assert.Contains("embedding-a", handler.Requests.Last().Body);
        Assert.Empty((await factory.LoadDraftAsync(AiProviderType.OllamaLocal)).ApiKey);
        Assert.False(factory.CreateTemporaryEmbedding(await factory.LoadDraftAsync(AiProviderType.OpenAI)) is null);
        Assert.Throws<InvalidOperationException>(() => factory.CreateTemporaryEmbedding(new(AiProviderType.Groq, "key-b", "chat", "pretend-embedding")));
    }

    [Fact]
    public async Task LateProviderLoadCannotOverwriteCurrentDraftAndUnsavedKeyIsCleared()
    {
        var delayed = new TaskCompletionSource<ProviderConfigDraft>(TaskCreationOptions.RunContinuationsAsynchronously);
        var providers = new FakeProviders { Load = p => p == AiProviderType.OpenAI ? delayed.Task : Task.FromResult(new ProviderConfigDraft(p, "", "groq-chat", "", "https://groq.test/v1", .31)) };
        var viewModel = new SettingsViewModel(providers) { ApiKey = "unsaved-key-a" };
        viewModel.SelectedProvider = AiProviderType.OpenAI;
        Assert.Empty(viewModel.ApiKey); Assert.True(viewModel.IsLoading);
        viewModel.SelectedProvider = AiProviderType.Groq;
        await WaitUntilAsync(() => !viewModel.IsLoading);
        delayed.SetResult(new(AiProviderType.OpenAI, "saved-key-a", "openai-chat", "embedding-a", Temperature: .88));
        await Task.Delay(50);
        Assert.Empty(viewModel.ApiKey); Assert.Equal("groq-chat", viewModel.ChatModel); Assert.Equal(.31, viewModel.Temperature);
        await viewModel.TestConnectionCommand.ExecuteAsync(null); Assert.Equal(0, providers.Saves);
        viewModel.Dispose();
    }

    [Fact]
    public void DpapiNeverAcceptsPlaintextBase64OrMalformedCiphertext()
    {
        foreach (var invalid in new[] { "plain-key", Convert.ToBase64String(Encoding.UTF8.GetBytes("plain-key")), Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) })
            Assert.Throws<InvalidOperationException>(() => WindowsDpapiSecretManager.Decrypt(invalid));
        Assert.DoesNotContain("sensitive", new ProviderConfigDraft(AiProviderType.OpenAI, "sensitive", "chat").ToString());
    }

    [Fact]
    public async Task UnreadableKeyLoadsOtherDraftFieldsAndCanBeReplacedExplicitly()
    {
        using var fixture = new SqliteFixture(); using var http = new HttpClient(new FakeHandler());
        var factory = new ChatProviderFactory(fixture, http, new WindowsDpapiSecretProtector());
        await using (var db = fixture.CreateDbContext())
        {
            db.ProviderSettings.Add(new() { ProviderType = AiProviderType.OpenAI, ChatModel = "saved-chat", EmbeddingModel = "saved-embed",
                BaseUrl = "https://proxy.test/v1", Temperature = .39, EncryptedApiKey = "not-dpapi", IsActive = true });
            await db.SaveChangesAsync();
        }
        var draft = await factory.LoadDraftAsync(AiProviderType.OpenAI);
        Assert.True(draft.KeyNeedsReplacement); Assert.Empty(draft.ApiKey); Assert.Equal("saved-chat", draft.ChatModel); Assert.Equal(.39, draft.Temperature);
        await Assert.ThrowsAsync<InvalidOperationException>(() => factory.GetChatSessionAsync());
        await factory.SaveAsync(draft with { ApiKey = "replacement-key", KeyNeedsReplacement = false });
        Assert.Equal("replacement-key", (await factory.LoadDraftAsync(AiProviderType.OpenAI)).ApiKey);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DirectAndRagSendSavedTemperatureInRealHttpPayload(bool useRag)
    {
        using var fixture = new SqliteFixture(); var handler = new FakeHandler(); using var http = new HttpClient(handler);
        handler.Response = _ => handler.Requests.Last().Url.AbsolutePath.EndsWith("/embeddings")
            ? new(HttpStatusCode.OK) { Content = new StringContent("{\"data\":[{\"embedding\":[1,0]}]}") }
            : new(HttpStatusCode.OK) { Content = new StringContent("{\"choices\":[{\"message\":{\"content\":\"answer\"}}]}") };
        var factory = new ChatProviderFactory(fixture, http, new FakeSecrets());
        await factory.SaveAsync(new(AiProviderType.OpenAI, "key", "chat", "embed", Temperature: .37, UseForEmbedding: true));
        var rag = new DocuMind.Application.Services.RagOrchestratorService(factory, new FakeParser(), new EfDocumentStore(fixture));
        if (useRag)
        {
            await rag.IngestDocumentAsync(SqliteFixture.WorkspaceId, fixture.File());
            Assert.Single((await rag.ChatWithRagAsync(SqliteFixture.WorkspaceId, "system", "question")).Sources);
        }
        else await rag.ChatExternalAsync("system", "question");
        using var body = JsonDocument.Parse(handler.Requests.Last().Body);
        Assert.Equal(.37, body.RootElement.GetProperty("temperature").GetDouble());
    }

    [Theory]
    [InlineData(AiProviderType.OpenAI)]
    [InlineData(AiProviderType.Groq)]
    [InlineData(AiProviderType.OllamaLocal)]
    [InlineData(AiProviderType.Gemini)]
    [InlineData(AiProviderType.OpenRouter)]
    public async Task HttpPayloadContainsTemperatureAndNoKeyInUrl(AiProviderType provider)
    {
        using var fixture = new SqliteFixture(); var handler = new FakeHandler(); using var http = new HttpClient(handler);
        if (provider == AiProviderType.Gemini) handler.Response = _ => new(HttpStatusCode.OK) { Content = new StringContent("{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"ok\"}]}}]}") };
        var factory = new ChatProviderFactory(fixture, http, new FakeSecrets());
        await factory.SaveAsync(new(provider, "secret", "configured-model", Temperature: .41));
        var session = await factory.GetChatSessionAsync();
        await session.Service.GenerateResponseAsync("system", "question", session.Temperature);
        var request = Assert.Single(handler.Requests); Assert.DoesNotContain("secret", request.Url.ToString());
        Assert.Equal("secret", request.Key);
        using var json = JsonDocument.Parse(request.Body);
        var temperature = provider == AiProviderType.Gemini ? json.RootElement.GetProperty("generationConfig").GetProperty("temperature") : json.RootElement.GetProperty("temperature");
        Assert.Equal(.41, temperature.GetDouble());
    }

    [Theory]
    [InlineData(AiProviderType.OpenAI)]
    [InlineData(AiProviderType.Gemini)]
    [InlineData(AiProviderType.OllamaLocal)]
    [InlineData(AiProviderType.OpenRouter)]
    public async Task EmbeddingUsesConfiguredEndpointAndModel(AiProviderType provider)
    {
        using var fixture = new SqliteFixture(); var handler = new FakeHandler(); using var http = new HttpClient(handler);
        handler.Response = _ => new(HttpStatusCode.OK) { Content = new StringContent(provider == AiProviderType.Gemini ? "{\"embedding\":{\"values\":[1,0]}}" : "{\"data\":[{\"embedding\":[1,0]}]}") };
        var factory = new ChatProviderFactory(fixture, http, new FakeSecrets());
        var embedding = factory.CreateTemporaryEmbedding(new(provider, "key", "chat", "embedding-chosen", "https://proxy.test/api"));
        Assert.Equal(new float[] { 1, 0 }, await embedding.GenerateEmbeddingAsync("sample"));
        var request = Assert.Single(handler.Requests); Assert.StartsWith("https://proxy.test/api/", request.Url.ToString());
        Assert.Contains("embedding-chosen", request.Body); Assert.DoesNotContain("?key", request.Url.ToString());
    }

    [Theory]
    [InlineData(401, 1)]
    [InlineData(403, 1)]
    [InlineData(429, 3)]
    [InlineData(503, 3)]
    public async Task EmbeddingRetryIsBoundedAndDoesNotRetryAuthentication(int status, int attempts)
    {
        var handler = new FakeHandler { Response = _ => new((HttpStatusCode)status) { Content = new StringContent("secret response") } };
        using var http = new HttpClient(handler);
        var service = new OpenAiCompatibleService(http, "secret", "https://fake.test/v1", "chat", AiProviderType.OpenAI);
        var error = await Assert.ThrowsAsync<HttpRequestException>(() => service.GenerateEmbeddingAsync("text"));
        Assert.Equal(attempts, handler.Requests.Count); Assert.DoesNotContain("secret", error.Message);
    }

    internal static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition()) await Task.Delay(10, timeout.Token);
    }
}
