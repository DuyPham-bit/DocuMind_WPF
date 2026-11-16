using DocuMind.Application.Interfaces;
using DocuMind.Application.Services;
using DocuMind.Domain.Entities;
using DocuMind.Infrastructure.Persistence;
using DocuMind.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DocuMind.Tests;

/// <summary>
/// Nhãn trạng thái tài liệu phải phân biệt: dùng được, đang nạp, nạp lỗi, và đổi model embedding;
/// nhãn lỗi/cần nạp lại phải bấm được để nạp lại từ file gốc.
/// </summary>
public class DocumentIndexStateTests
{
    [Fact]
    public async Task StoreReportsReadyFailedIndexingAndProfileMismatchSeparately()
    {
        using var fixture = new SqliteFixture(); var providers = new FakeProviders(); var store = new EfDocumentStore(fixture);
        var rag = new RagOrchestratorService(providers, new FakeParser(), store);
        await rag.IngestDocumentAsync(SqliteFixture.WorkspaceId, fixture.File("ready.txt"));
        var failed = await store.BeginAsync(SqliteFixture.WorkspaceId, fixture.File("failed.txt"), CancellationToken.None);
        await store.FailAsync(failed.Id);
        await store.BeginAsync(SqliteFixture.WorkspaceId, fixture.File("indexing.txt"), CancellationToken.None);

        var files = (await rag.GetWorkspaceFilesAsync(SqliteFixture.WorkspaceId)).ToDictionary(f => f.DisplayName);
        Assert.Equal(DocumentIndexState.Ready, files["ready.txt"].IndexState);
        Assert.Equal(DocumentIndexState.Failed, files["failed.txt"].IndexState);
        Assert.Equal(DocumentIndexState.Indexing, files["indexing.txt"].IndexState);
        Assert.False(files["ready.txt"].NeedsReindex);
        Assert.True(files["failed.txt"].NeedsReindex && files["failed.txt"].CanReindex);
        Assert.True(files["indexing.txt"].NeedsReindex);
        Assert.False(files["indexing.txt"].CanReindex); // đang nạp thì không cho bấm nạp lại chồng lên

        providers.EmbeddingModel = "embedding-b"; // người dùng lưu model embedding khác
        var afterSwitch = (await rag.GetWorkspaceFilesAsync(SqliteFixture.WorkspaceId)).ToDictionary(f => f.DisplayName);
        Assert.Equal(DocumentIndexState.NeedsReindex, afterSwitch["ready.txt"].IndexState);
        Assert.Equal(DocumentIndexState.Failed, afterSwitch["failed.txt"].IndexState);
    }

    [Fact]
    public async Task FailedReindexOfAReadyDocumentStillReportsReadyBecauseTheOldIndexIsUsable()
    {
        using var fixture = new SqliteFixture(); var providers = new FakeProviders(); var store = new EfDocumentStore(fixture);
        var rag = new RagOrchestratorService(providers, new FakeParser(), store);
        var path = fixture.File("lecture.txt");
        await rag.IngestDocumentAsync(SqliteFixture.WorkspaceId, path);
        providers.Ai.Embed = (_, _) => throw new HttpRequestException("fake outage");
        await Assert.ThrowsAnyAsync<Exception>(() => rag.IngestDocumentAsync(SqliteFixture.WorkspaceId, path));

        Assert.Equal(DocumentIndexState.Ready, Assert.Single(await rag.GetWorkspaceFilesAsync(SqliteFixture.WorkspaceId)).IndexState);
    }

    [Fact]
    public async Task ReindexCommandIngestsFailedDocumentFromItsOriginalFile()
    {
        using var fixture = new SqliteFixture(); var providers = new FakeProviders(); var store = new EfDocumentStore(fixture);
        var path = fixture.File("retry.txt");
        var staging = await store.BeginAsync(SqliteFixture.WorkspaceId, path, CancellationToken.None);
        await store.FailAsync(staging.Id);
        var vm = new MainViewModel(new(providers, new FakeParser(), store), providers, new EfRepository<Workspace>(fixture), new EfChatHistoryStore(fixture));
        await vm.InitializeAsync(); await ProviderRegressionTests.WaitUntilAsync(() => !vm.IsLoading);
        var failed = Assert.Single(vm.UploadedFiles);
        Assert.Equal(DocumentIndexState.Failed, failed.IndexState);

        Assert.True(vm.ReindexDocumentCommand.CanExecute(failed));
        await vm.ReindexDocumentCommand.ExecuteAsync(failed);

        Assert.Equal(DocumentIndexState.Ready, Assert.Single(vm.UploadedFiles).IndexState);
        Assert.Contains("Completed 1", vm.BusyStatus);
    }

    [Fact]
    public async Task MainWindowShowsClickableRetryOnlyForFailedDocuments()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                using var fixture = new SqliteFixture(); var providers = new FakeProviders();
                var vm = new MainViewModel(new(providers, new FakeParser(), new EfDocumentStore(fixture)), providers,
                    new EfRepository<Workspace>(fixture), new EfChatHistoryStore(fixture));
                using var services = new ServiceCollection().BuildServiceProvider();
                var main = new DocuMind.WPF.MainWindow(vm, services);
                main.Show();
                vm.UploadedFiles.Clear();
                vm.UploadedFiles.Add(new(Guid.NewGuid(), "ok.pdf", "C:\\ok.pdf", false, DocumentIndexState.Ready));
                vm.UploadedFiles.Add(new(Guid.NewGuid(), "bad.pdf", "C:\\bad.pdf", true, DocumentIndexState.Failed));
                main.UpdateLayout();
                var buttons = Descendants(main).OfType<System.Windows.Controls.Button>()
                    .Where(b => b.Name == "DocReindexButton").ToList();
                Assert.Equal(2, buttons.Count);
                var visible = Assert.Single(buttons, b => b.IsVisible);
                Assert.Equal("Index failed · Retry ↻", visible.Content);
                Assert.Same(vm.UploadedFiles[1], visible.CommandParameter);
                main.Close();
                completion.SetResult();
            }
            catch (Exception error) { completion.SetException(error); }
            finally { System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown(); }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(20));
    }

    private static IEnumerable<System.Windows.DependencyObject> Descendants(System.Windows.DependencyObject owner)
    {
        for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(owner); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(owner, i);
            yield return child;
            foreach (var d in Descendants(child)) yield return d;
        }
    }

    [Fact]
    public async Task ReindexCommandExplainsMissingOriginalFileWithoutCallingTheProvider()
    {
        using var fixture = new SqliteFixture(); var providers = new FakeProviders(); var store = new EfDocumentStore(fixture);
        var path = fixture.File("moved.txt");
        var staging = await store.BeginAsync(SqliteFixture.WorkspaceId, path, CancellationToken.None);
        await store.FailAsync(staging.Id);
        File.Delete(path);
        var embedCalls = 0;
        providers.Ai.Embed = (_, _) => { embedCalls++; return Task.FromResult(new float[] { 1, 0 }); };
        var vm = new MainViewModel(new(providers, new FakeParser(), store), providers, new EfRepository<Workspace>(fixture), new EfChatHistoryStore(fixture));
        await vm.InitializeAsync(); await ProviderRegressionTests.WaitUntilAsync(() => !vm.IsLoading);

        await vm.ReindexDocumentCommand.ExecuteAsync(Assert.Single(vm.UploadedFiles));

        Assert.Contains("Original file not found", vm.BusyStatus);
        Assert.Equal(0, embedCalls);
        Assert.Equal(DocumentIndexState.Failed, Assert.Single(vm.UploadedFiles).IndexState);
    }
}
