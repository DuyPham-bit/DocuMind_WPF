using DocuMind.Application.DTOs;
using DocuMind.Application.Interfaces;
using DocuMind.Application.Services;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;
using DocuMind.Infrastructure.AiProviders;
using DocuMind.Infrastructure.Persistence;
using DocuMind.WPF.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Tests;

public class WorkspaceRegressionTests
{
    [Fact]
    public async Task LateWorkspaceReloadCannotPutFilesOrChatFromAIntoB()
    {
        using var fixture = new SqliteFixture(); var repository = new EfRepository<Workspace>(fixture);
        var a = (await repository.GetAllAsync()).Single(); var b = new Workspace { Name = "B" }; await repository.AddAsync(b);
        var history = new EfChatHistoryStore(fixture);
        await history.AppendAsync(new() { WorkspaceId = b.Id, ConversationId = b.ConversationId, Content = "question B" },
            new() { WorkspaceId = b.Id, ConversationId = b.ConversationId, Role = MessageRole.Assistant, Content = "answer B", Timestamp = DateTime.UtcNow.AddSeconds(1) });
        var delayed = new TaskCompletionSource<List<DocumentListItem>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var documents = new DelayedDocumentStore(new EfDocumentStore(fixture)) { List = id => id == a.Id ? delayed.Task : Task.FromResult(new List<DocumentListItem> { new(Guid.NewGuid(), "B.txt", "B.txt") }) };
        var providers = new FakeProviders(); var vm = new MainViewModel(new(providers, new FakeParser(), documents), providers, repository, history);
        vm.SelectedWorkspace = a; Assert.True(vm.IsLoading);
        vm.SelectedWorkspace = b; await ProviderRegressionTests.WaitUntilAsync(() => !vm.IsLoading);
        delayed.SetResult([new(Guid.NewGuid(), "A.txt", "A.txt")]); await Task.Delay(50);
        Assert.Equal("B.txt", Assert.Single(vm.UploadedFiles).DisplayName);
        Assert.Equal(new[] { "question B", "answer B" }, vm.ChatMessages.Select(m => m.Content));
    }

    [Fact]
    public async Task InFlightChatPersistsToCapturedWorkspaceAndMetadataWithoutUpdatingNewSelection()
    {
        using var fixture = new SqliteFixture(); var repository = new EfRepository<Workspace>(fixture);
        var a = (await repository.GetAllAsync()).Single(); var b = new Workspace { Name = "B" }; await repository.AddAsync(b);
        var history = new EfChatHistoryStore(fixture); var providers = new FakeProviders();
        var response = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        providers.Ai.Respond = _ => response.Task;
        var vm = new MainViewModel(new(providers, new FakeParser(), new EfDocumentStore(fixture)), providers, repository, history) { UseRagMode = false, SelectedWorkspace = a };
        await ProviderRegressionTests.WaitUntilAsync(() => !vm.IsLoading);
        vm.UserInput = "question A"; var send = vm.SendMessageCommand.ExecuteAsync(null);
        await ProviderRegressionTests.WaitUntilAsync(() => providers.Ai.Requests.Count == 1);
        vm.SelectedWorkspace = b; vm.UseRagMode = true; providers.Temperature = .99;
        await ProviderRegressionTests.WaitUntilAsync(() => !vm.IsLoading);
        response.SetResult("answer A"); await send;
        Assert.Empty(vm.ChatMessages); Assert.False(vm.IsBusy);
        var saved = await history.LoadAsync(a.Id, a.ConversationId);
        Assert.Equal(2, saved.Count); Assert.All(saved, m => { Assert.Equal(ChatMode.Direct, m.Mode); Assert.Equal(.23, m.Temperature); Assert.Equal("OpenAI", m.Provider); });
        Assert.Empty(await history.LoadAsync(b.Id, b.ConversationId));
        vm.SelectedWorkspace = a; await ProviderRegressionTests.WaitUntilAsync(() => !vm.IsLoading);
        var export = vm.BuildMarkdownExport(); Assert.Contains("Mode: Direct", export); Assert.Contains("Temperature: 0.23", export);
        var previousConversation = a.ConversationId;
        await vm.ClearChatCommand.ExecuteAsync(null);
        Assert.Empty(vm.ChatMessages); Assert.NotEqual(previousConversation, a.ConversationId);
        Assert.Equal(2, (await history.LoadAsync(a.Id, previousConversation)).Count);
    }

    [Fact]
    public async Task BatchIngestReportsActualSuccessAndFailurePerFile()
    {
        using var fixture = new SqliteFixture(); var repository = new EfRepository<Workspace>(fixture);
        var providers = new FakeProviders(); var calls = 0;
        providers.Ai.Embed = (_, _) => ++calls == 2 ? throw new HttpRequestException("fake failure") : Task.FromResult(new float[] { 1, 0 });
        var vm = new MainViewModel(new(providers, new FakeParser(), new EfDocumentStore(fixture)), providers, repository, new EfChatHistoryStore(fixture))
            { SelectedWorkspace = (await repository.GetAllAsync()).Single() };
        await ProviderRegressionTests.WaitUntilAsync(() => !vm.IsLoading);
        await vm.IngestFilesAsync([fixture.File("first.txt"), fixture.File("failed.txt"), fixture.File("last.txt")]);
        Assert.Contains("Completed 2", vm.BusyStatus); Assert.Contains("Failed or canceled 1", vm.BusyStatus); Assert.Contains("failed.txt", vm.BusyStatus);
        await using var db = fixture.CreateDbContext();
        Assert.Equal(2, await db.DocumentVersions.CountAsync(v => v.Status == DocumentVersionStatus.Ready));
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task WorkspaceRenameAndSystemPromptPersistAndKeepSelection()
    {
        using var fixture = new SqliteFixture(); var providers = new FakeProviders(); var repository = new EfRepository<Workspace>(fixture);
        var vm = new MainViewModel(new(providers, new FakeParser(), new EfDocumentStore(fixture)), providers, repository, new EfChatHistoryStore(fixture));
        await vm.InitializeAsync(); await ProviderRegressionTests.WaitUntilAsync(() => !vm.IsLoading);
        var id = vm.SelectedWorkspace!.Id;
        vm.WorkspaceName = "Renamed"; vm.SystemPrompt = "Saved system instruction";
        await vm.SaveWorkspaceCommand.ExecuteAsync(null); await ProviderRegressionTests.WaitUntilAsync(() => !vm.IsLoading);
        Assert.Equal(id, vm.SelectedWorkspace!.Id); Assert.Equal("Renamed", vm.SelectedWorkspace.Name);
        var persisted = await repository.GetByIdAsync(id);
        Assert.Equal("Saved system instruction", persisted!.SystemPrompt); Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task HistoryBudgetKeepsNewestCompleteTurnsAndSeparatesConversations()
    {
        using var fixture = new SqliteFixture(); var history = new EfChatHistoryStore(fixture); var conversation = Guid.NewGuid();
        for (var i = 0; i < 3; i++)
            await history.AppendAsync(new() { WorkspaceId = SqliteFixture.WorkspaceId, ConversationId = conversation, Content = $"question{i}", Timestamp = DateTime.UtcNow.AddSeconds(i * 2) },
                new() { WorkspaceId = SqliteFixture.WorkspaceId, ConversationId = conversation, Role = MessageRole.Assistant, Content = $"answer{i}", Timestamp = DateTime.UtcNow.AddSeconds(i * 2 + 1) });
        var loaded = await history.LoadAsync(SqliteFixture.WorkspaceId, conversation, 55);
        Assert.Equal(new[] { "question2", "answer2" }, loaded.Select(m => m.Content));
        Assert.Empty(await history.LoadAsync(SqliteFixture.WorkspaceId, Guid.NewGuid()));
    }

    [Fact]
    public async Task LongVietnameseAnswerIsStillShownAfterReload()
    {
        using var fixture = new SqliteFixture(); var repository = new EfRepository<Workspace>(fixture); var history = new EfChatHistoryStore(fixture);
        var workspace = (await repository.GetAllAsync()).Single();
        var longAnswer = string.Concat(Enumerable.Repeat("Tóm tắt nội dung chương học. ", 400)); // well over the model-context byte budget
        await history.AppendAsync(new() { WorkspaceId = workspace.Id, ConversationId = workspace.ConversationId, Content = "câu hỏi 1" },
            new() { WorkspaceId = workspace.Id, ConversationId = workspace.ConversationId, Role = MessageRole.Assistant, Content = "trả lời 1", Timestamp = DateTime.UtcNow.AddSeconds(1) });
        await history.AppendAsync(new() { WorkspaceId = workspace.Id, ConversationId = workspace.ConversationId, Content = "tóm tắt giúp mình", Timestamp = DateTime.UtcNow.AddSeconds(2) },
            new() { WorkspaceId = workspace.Id, ConversationId = workspace.ConversationId, Role = MessageRole.Assistant, Content = longAnswer, Timestamp = DateTime.UtcNow.AddSeconds(3) });
        var providers = new FakeProviders();
        var vm = new MainViewModel(new(providers, new FakeParser(), new EfDocumentStore(fixture)), providers, repository, history) { SelectedWorkspace = workspace };
        await ProviderRegressionTests.WaitUntilAsync(() => !vm.IsLoading);
        Assert.Equal(new[] { "câu hỏi 1", "trả lời 1", "tóm tắt giúp mình", longAnswer }, vm.ChatMessages.Select(m => m.Content));
    }

    [Fact]
    public async Task FailedSendRestoresQuestionAndDoesNotLeaveUnsavedBubble()
    {
        using var fixture = new SqliteFixture(); var repository = new EfRepository<Workspace>(fixture); var history = new EfChatHistoryStore(fixture);
        var providers = new FakeProviders(); providers.Ai.Respond = _ => throw new HttpRequestException("fake failure");
        var vm = new MainViewModel(new(providers, new FakeParser(), new EfDocumentStore(fixture)), providers, repository, history)
            { UseRagMode = false, SelectedWorkspace = (await repository.GetAllAsync()).Single() };
        await ProviderRegressionTests.WaitUntilAsync(() => !vm.IsLoading);
        vm.UserInput = "question that fails"; await vm.SendMessageCommand.ExecuteAsync(null);
        Assert.Empty(vm.ChatMessages); Assert.Equal("question that fails", vm.UserInput); Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task ValidationErrorsReachTheUserInsteadOfAGenericMessage()
    {
        using var fixture = new SqliteFixture(); using var http = new HttpClient(new FakeHandler());
        var factory = new ChatProviderFactory(fixture, http, new FakeSecrets());
        var missingKey = await Assert.ThrowsAsync<UserFacingException>(() => factory.SaveAsync(new(AiProviderType.OpenAI, "", "gpt-4o-mini")));
        Assert.Contains("API key", UserErrors.Describe(missingKey));
        var badUrl = Assert.Throws<UserFacingException>(() => factory.CreateTemporary(new(AiProviderType.OpenAI, "key", "chat", BaseUrl: "http://example.com/v1")));
        Assert.Contains("HTTPS", UserErrors.Describe(badUrl));
    }

    [Fact]
    public async Task CancelCommandStopsEmbeddingRestoresUiAndRetainsReadyVersion()
    {
        using var fixture = new SqliteFixture(); var providers = new FakeProviders(); var parser = new FakeParser(); var store = new EfDocumentStore(fixture);
        var rag = new RagOrchestratorService(providers, parser, store); var path = fixture.File();
        await rag.IngestDocumentAsync(SqliteFixture.WorkspaceId, path);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        providers.Ai.Embed = async (_, token) => { entered.SetResult(); await Task.Delay(Timeout.Infinite, token); return [1, 0]; };
        var repo = new EfRepository<Workspace>(fixture);
        var vm = new MainViewModel(rag, providers, repo, new EfChatHistoryStore(fixture)) { SelectedWorkspace = (await repo.GetAllAsync()).Single() };
        await ProviderRegressionTests.WaitUntilAsync(() => !vm.IsLoading);
        var ingest = vm.IngestFilesAsync([path]); await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        vm.CancelCommand.Execute(null); await ingest;
        Assert.False(vm.IsBusy); Assert.Contains("canceled or timed out", vm.BusyStatus);
        providers.Ai.Embed = (_, _) => Task.FromResult(new float[] { 1, 0 });
        Assert.Equal("old content", Assert.Single((await rag.ChatWithRagAsync(SqliteFixture.WorkspaceId, "system", "question")).Sources).Content);
    }

    [Fact]
    public async Task SavingProviderAutomaticallyReindexesPendingDocuments()
    {
        using var fixture = new SqliteFixture();
        var repository = new EfRepository<Workspace>(fixture);
        var store = new EfDocumentStore(fixture);
        var providers = new FakeProviders();
        var path = fixture.File("pending.txt");

        // Simulate a document whose earlier ingest failed (Staging -> Failed) before API key was fixed
        var staging = await store.BeginAsync(SqliteFixture.WorkspaceId, path, CancellationToken.None);
        await store.FailAsync(staging.Id);

        var vm = new MainViewModel(new(providers, new FakeParser(), store), providers, repository, new EfChatHistoryStore(fixture));
        await vm.InitializeAsync();
        await ProviderRegressionTests.WaitUntilAsync(() => !vm.IsLoading);
        Assert.True(Assert.Single(vm.UploadedFiles).NeedsReindex);

        // Closing settings without saving does not auto-ingest
        await vm.OnProviderSettingsClosedAsync(savedSuccessfully: false);
        Assert.True(Assert.Single(vm.UploadedFiles).NeedsReindex);

        // Closing settings after saving an embedding-capable provider auto-ingests pending files
        await vm.OnProviderSettingsClosedAsync(savedSuccessfully: true);
        Assert.False(Assert.Single(vm.UploadedFiles).NeedsReindex);
        Assert.Contains("Completed 1", vm.BusyStatus);
    }

    [Fact]
    public async Task ClosingAppArchivesActiveConversationAndStartsNewConversationOnReopen()
    {
        using var fixture = new SqliteFixture();
        var repository = new EfRepository<Workspace>(fixture);
        var store = new EfDocumentStore(fixture);
        var history = new EfChatHistoryStore(fixture);
        var providers = new FakeProviders();

        var vm = new MainViewModel(new(providers, new FakeParser(), store), providers, repository, history);
        await vm.InitializeAsync();
        await ProviderRegressionTests.WaitUntilAsync(() => !vm.IsLoading);

        var initialConversation = vm.SelectedWorkspace!.ConversationId;
        await history.AppendAsync(
            new() { WorkspaceId = vm.SelectedWorkspace.Id, ConversationId = initialConversation, Content = "Câu hỏi trước khi tắt", Timestamp = DateTime.UtcNow },
            new() { WorkspaceId = vm.SelectedWorkspace.Id, ConversationId = initialConversation, Role = MessageRole.Assistant, Content = "Trả lời trước khi tắt", Timestamp = DateTime.UtcNow.AddSeconds(1) });

        // Simulate app close -> archive current conversation and rotate to a new ConversationId
        await vm.StartNewConversationOnCloseAsync();
        Assert.NotEqual(initialConversation, vm.SelectedWorkspace.ConversationId);

        // Calling close-archive when the new conversation is empty keeps the empty ConversationId
        var freshId = vm.SelectedWorkspace.ConversationId;
        await vm.StartNewConversationOnCloseAsync();
        Assert.Equal(freshId, vm.SelectedWorkspace.ConversationId);

        // Simulate reopening the app with a fresh MainViewModel
        var reopened = new MainViewModel(new(providers, new FakeParser(), store), providers, repository, history);
        await reopened.InitializeAsync();
        await ProviderRegressionTests.WaitUntilAsync(() => !reopened.IsLoading);

        Assert.Empty(reopened.ChatMessages);
        var archived = Assert.Single(reopened.Conversations);
        Assert.Equal(initialConversation, archived.ConversationId);
        Assert.Equal(2, archived.MessageCount);
        Assert.Contains("Câu hỏi trước khi tắt", archived.Title);

        // Selecting the saved conversation restores its messages
        await reopened.OpenConversationCommand.ExecuteAsync(archived);
        await ProviderRegressionTests.WaitUntilAsync(() => !reopened.IsLoading);
        Assert.Equal(new[] { "Câu hỏi trước khi tắt", "Trả lời trước khi tắt" }, reopened.ChatMessages.Select(m => m.Content));
    }

    [Fact]
    public async Task SavingANewEmbeddingProviderReindexesDocumentsBuiltWithTheOldOne()
    {
        using var fixture = new SqliteFixture(); var repository = new EfRepository<Workspace>(fixture);
        var providers = new FakeProviders(); var store = new EfDocumentStore(fixture);
        var rag = new RagOrchestratorService(providers, new FakeParser(), store);
        await rag.IngestDocumentAsync(SqliteFixture.WorkspaceId, fixture.File("lecture.txt"));
        var vm = new MainViewModel(rag, providers, repository, new EfChatHistoryStore(fixture));
        await vm.InitializeAsync(); await ProviderRegressionTests.WaitUntilAsync(() => !vm.IsLoading);
        Assert.False(Assert.Single(vm.UploadedFiles).NeedsReindex);

        providers.EmbeddingModel = "embedding-b"; // the user saved a different embedding provider in Settings
        await vm.OnProviderSettingsClosedAsync(savedSuccessfully: true);

        Assert.False(Assert.Single(vm.UploadedFiles).NeedsReindex);
        Assert.Contains("Completed 1", vm.BusyStatus);
        Assert.Equal("old content", Assert.Single((await rag.ChatWithRagAsync(SqliteFixture.WorkspaceId, "system", "question")).Sources).Content);
    }

    [Fact]
    public async Task PendingDocumentsWithoutEmbeddingProviderExplainWhyNothingWasReindexed()
    {
        using var fixture = new SqliteFixture(); var repository = new EfRepository<Workspace>(fixture);
        var providers = new FakeProviders(); var store = new EfDocumentStore(fixture);
        var staging = await store.BeginAsync(SqliteFixture.WorkspaceId, fixture.File("pending.txt"), CancellationToken.None);
        await store.FailAsync(staging.Id);
        var vm = new MainViewModel(new(providers, new FakeParser(), store), providers, repository, new EfChatHistoryStore(fixture));
        await vm.InitializeAsync(); await ProviderRegressionTests.WaitUntilAsync(() => !vm.IsLoading);

        providers.EmbeddingEnabled = false;
        await vm.OnProviderSettingsClosedAsync(savedSuccessfully: true);

        Assert.True(Assert.Single(vm.UploadedFiles).NeedsReindex);
        Assert.Contains("need re-indexing", vm.BusyStatus);
    }

    private sealed class DelayedDocumentStore(IDocumentStore inner) : IDocumentStore
    {
        public required Func<Guid, Task<List<DocumentListItem>>> List;
        public Task<List<DocumentListItem>> ListAsync(Guid workspaceId, CancellationToken ct = default) => List(workspaceId);
        public Task<DocumentVersion> BeginAsync(Guid workspaceId, string path, CancellationToken ct) => inner.BeginAsync(workspaceId, path, ct);
        public Task ActivateAsync(Guid versionId, string hash, EmbeddingProfile profile, List<DocumentChunk> chunks, CancellationToken ct) => inner.ActivateAsync(versionId, hash, profile, chunks, ct);
        public Task FailAsync(Guid versionId) => inner.FailAsync(versionId);
        public Task<List<DocumentChunk>> GetCompatibleChunksAsync(Guid workspaceId, EmbeddingSession session, int dimension, CancellationToken ct) => inner.GetCompatibleChunksAsync(workspaceId, session, dimension, ct);
        public Task DeleteAsync(Guid workspaceId, Guid documentId, CancellationToken ct = default) => inner.DeleteAsync(workspaceId, documentId, ct);
    }
}
