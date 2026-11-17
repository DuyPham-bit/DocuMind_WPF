using DocuMind.Application.Common;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using DocuMind.Application.Services;
using DocuMind.Domain.Entities;
using DocuMind.Infrastructure.Persistence;
using DocuMind.WPF.ViewModels;
using DocuMind.WPF.Views;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Tests;

public class QuizDocumentSelectionTests
{
    private const string QuestionJson = """
        {"questions":[{"question":"Câu hỏi từ tài liệu đã chọn?","options":["Một","Hai","Ba","Bốn"],"answer":0,"source":1}]}
        """;

    [Theory]
    [InlineData("")]
    [InlineData("Chủ đề ôn tập")]
    public async Task SelectionFiltersBeforeSamplingOrRankingAndUsesIdsEvenWhenFileNamesMatch(string topic)
    {
        using var fixture = new SqliteFixture();
        var providers = new FakeProviders();
        var parser = new FakeParser();
        var rag = new RagOrchestratorService(providers, parser, new EfDocumentStore(fixture));
        // Excluded documents score higher than the selected files and outnumber topK.
        for (var i = 0; i < 8; i++)
        {
            parser.Pages = [(1, "excluded-marker " + i)];
            await rag.IngestDocumentAsync(SqliteFixture.WorkspaceId, fixture.File($"excluded-{i}/notes.txt"));
        }
        providers.Ai.Embed = (_, _) => Task.FromResult(new float[] { .8f, .6f });
        foreach (var name in new[] { "alpha", "beta" })
        {
            parser.Pages = [(2, "selected-marker-" + name)];
            await rag.IngestDocumentAsync(SqliteFixture.WorkspaceId, fixture.File($"selected-{name}/notes.txt"));
        }
        providers.Ai.Embed = (_, _) => Task.FromResult(new float[] { 1, 0 });
        providers.Ai.Respond = _ => Task.FromResult(QuestionJson);
        await using var db = fixture.CreateDbContext();
        var selectedIds = await db.Documents.Where(d => d.SourcePath.Contains("selected-")).Select(d => d.Id).ToArrayAsync();
        var quiz = new QuizService(rag, providers, new EfQuizStore(fixture));

        var draft = await quiz.GenerateAsync(SqliteFixture.WorkspaceId, topic, 3, documentIds: selectedIds);

        var request = Assert.Single(providers.Ai.Requests);
        Assert.Contains("selected-marker-alpha", request.User);
        Assert.Contains("selected-marker-beta", request.User);
        Assert.DoesNotContain("excluded-marker", request.User);
        Assert.Contains(draft.Questions.Single().Source!.DocumentId!.Value, selectedIds);
        if (topic.Length == 0) Assert.Equal(AppText.T("Selected documents", "Tài liệu đã chọn"), draft.Topic);
    }

    [Fact]
    public async Task EmptySelectionNeverFallsBackToAllDocumentsOrCallsAi()
    {
        using var fixture = new SqliteFixture();
        var providers = new FakeProviders();
        var rag = new RagOrchestratorService(providers, new FakeParser(), new EfDocumentStore(fixture));
        await rag.IngestDocumentAsync(SqliteFixture.WorkspaceId, fixture.File());
        providers.Ai.Embed = (_, _) => throw new Exception("An empty selection must not call embedding.");
        var quiz = new QuizService(rag, providers, new EfQuizStore(fixture));

        var error = await Assert.ThrowsAsync<QuizUnavailableException>(() => quiz.GenerateAsync(SqliteFixture.WorkspaceId, "", 3, documentIds: []));

        Assert.Contains(AppText.T("at least one document", "chọn ít nhất"), error.Message);
        Assert.Empty(providers.Ai.Requests);
    }

    [Theory]
    [InlineData("")]
    [InlineData("DI")]
    public async Task SelectedDocumentFromAnotherWorkspaceCannotBeUsed(string topic)
    {
        using var fixture = new SqliteFixture();
        var providers = new FakeProviders();
        var rag = new RagOrchestratorService(providers, new FakeParser(), new EfDocumentStore(fixture));
        var otherWorkspace = new Workspace { Name = "Other" };
        await new EfRepository<Workspace>(fixture).AddAsync(otherWorkspace);
        await rag.IngestDocumentAsync(otherWorkspace.Id, fixture.File("other.txt"));
        await using var db = fixture.CreateDbContext();
        var otherDocumentId = await db.Documents.Select(d => d.Id).SingleAsync();
        var quiz = new QuizService(rag, providers, new EfQuizStore(fixture));

        await Assert.ThrowsAsync<QuizUnavailableException>(() => quiz.GenerateAsync(SqliteFixture.WorkspaceId, topic, 3, documentIds: [otherDocumentId]));

        Assert.Empty(providers.Ai.Requests);
    }

    [Fact]
    public async Task ViewModelSelectsReadyFilesDisablesEmptySelectionAndPassesChosenFileToGeneration()
    {
        using var fixture = new SqliteFixture();
        var providers = new FakeProviders();
        var parser = new FakeParser();
        var store = new EfDocumentStore(fixture);
        var rag = new RagOrchestratorService(providers, parser, store);
        foreach (var name in new[] { "alpha", "beta" })
        {
            parser.Pages = [(1, name + "-marker")];
            await rag.IngestDocumentAsync(SqliteFixture.WorkspaceId, fixture.File(name + ".txt"));
        }
        var pending = await store.BeginAsync(SqliteFixture.WorkspaceId, fixture.File("pending.txt"), default);
        await store.FailAsync(pending.Id);
        providers.Ai.Respond = _ => Task.FromResult(QuestionJson);
        var vm = new QuizViewModel(new QuizService(rag, providers, new EfQuizStore(fixture)));
        await vm.InitializeAsync(SqliteFixture.WorkspaceId, "WS");

        Assert.Equal(3, vm.Documents.Count);
        Assert.Equal(2, vm.Documents.Count(d => d.IsSelected));
        Assert.False(vm.Documents.Single(d => d.Name == "pending.txt").IsAvailable);
        vm.ClearDocumentSelectionCommand.Execute(null);
        Assert.False(vm.GenerateCommand.CanExecute(null));
        vm.SelectAllDocumentsCommand.Execute(null);
        Assert.Equal(2, vm.Documents.Count(d => d.IsSelected));
        vm.Documents.Single(d => d.Name == "beta.txt").IsSelected = false;
        Assert.Contains("1/2", vm.DocumentSelectionSummary);

        var response = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        providers.Ai.Respond = _ => response.Task;
        var generation = vm.GenerateCommand.ExecuteAsync(null);
        Assert.True(vm.IsBusy);
        Assert.False(vm.IsSelectionEditable);
        Assert.False(vm.SelectAllDocumentsCommand.CanExecute(null));
        response.SetResult(QuestionJson);
        await generation;

        Assert.False(vm.IsBusy);
        Assert.Single(vm.Questions);
        Assert.Equal("alpha.txt", vm.Questions[0].Question.Source!.FileName);
        var request = Assert.Single(providers.Ai.Requests);
        Assert.Contains("alpha-marker", request.User);
        Assert.DoesNotContain("beta-marker", request.User);
    }

    [Fact]
    public async Task EmptyOrIncompatibleWorkspaceDoesNotEnableGenerate()
    {
        using var fixture = new SqliteFixture();
        var providers = new FakeProviders();
        var rag = new RagOrchestratorService(providers, new FakeParser(), new EfDocumentStore(fixture));
        var vm = new QuizViewModel(new QuizService(rag, providers, new EfQuizStore(fixture)));
        await vm.InitializeAsync(SqliteFixture.WorkspaceId, "WS");
        Assert.False(vm.GenerateCommand.CanExecute(null));
        Assert.Contains(AppText.T("no documents yet", "chưa có tài liệu"), vm.DocumentSelectionSummary);
        await rag.IngestDocumentAsync(SqliteFixture.WorkspaceId, fixture.File());
        providers.EmbeddingModel = "new-model";
        await vm.InitializeAsync(SqliteFixture.WorkspaceId, "WS");
        Assert.False(vm.GenerateCommand.CanExecute(null));
        Assert.All(vm.Documents, d => Assert.False(d.IsAvailable || d.IsSelected));
    }

    [Fact]
    public async Task DocumentPickerBindsCheckboxesAndFitsMinimumWindowSize()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                using var fixture = new SqliteFixture();
                var providers = new FakeProviders();
                var rag = new RagOrchestratorService(providers, new FakeParser(), new EfDocumentStore(fixture));
                rag.IngestDocumentAsync(SqliteFixture.WorkspaceId, fixture.File("first.txt")).GetAwaiter().GetResult();
                rag.IngestDocumentAsync(SqliteFixture.WorkspaceId, fixture.File("second.txt")).GetAwaiter().GetResult();
                var vm = new QuizViewModel(new QuizService(rag, providers, new EfQuizStore(fixture)));
                vm.InitializeAsync(SqliteFixture.WorkspaceId, "WS").GetAwaiter().GetResult();
                var window = new QuizWindow(vm, SqliteFixture.WorkspaceId, "WS");
                var root = (FrameworkElement)window.Content;
                root.DataContext = vm;
                root.Measure(new Size(728, 550));
                root.Arrange(new Rect(0, 0, 728, 550));
                root.UpdateLayout();
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
                var picker = (Border)window.FindName("DocumentPicker");
                var checkboxes = Descendants(picker).OfType<CheckBox>().ToList();
                Assert.Equal(2, checkboxes.Count);
                Assert.All(checkboxes, c => Assert.True(c.IsChecked));
                checkboxes[0].IsChecked = false;
                Assert.Single(vm.Documents, d => d.IsSelected);
                vm.IsBusy = true;
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
                Assert.All(checkboxes, c => Assert.False(c.IsEnabled));
                var questionScroll = (ScrollViewer)window.FindName("QuestionScroll");
                var pickerBounds = picker.TransformToAncestor(root).TransformBounds(new Rect(picker.RenderSize));
                var questionBounds = questionScroll.TransformToAncestor(root).TransformBounds(new Rect(questionScroll.RenderSize));
                Assert.True(pickerBounds.Bottom <= questionBounds.Top);
                Assert.True(questionBounds.Height > 100);
                window.Close();
                completion.SetResult();
            }
            catch (Exception ex) { completion.SetException(ex); }
            finally { Dispatcher.CurrentDispatcher.InvokeShutdown(); }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(20));
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject owner)
    {
        for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(owner); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(owner, i);
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
}
