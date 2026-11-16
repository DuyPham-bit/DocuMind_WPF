using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using DocuMind.Application.DTOs;
using DocuMind.Application.Services;
using DocuMind.Infrastructure.Persistence;
using DocuMind.WPF.Localization;
using DocuMind.WPF.ViewModels;
using DocuMind.WPF.Views;

namespace DocuMind.Tests;

/// <summary>Cửa sổ Quiz phải theo ngôn ngữ EN / VI của app như cửa sổ chính.</summary>
[Collection("Language")] // đổi công tắc ngôn ngữ dùng chung toàn process
public class QuizLocalizationTests : IDisposable
{
    [Theory]
    [InlineData(false, "DocuMind – Study quiz", "Submit", "↺ Try again", "Quiz documents", "Q1. Q?", "✅ Correct", "This workspace has no documents yet.")]
    [InlineData(true, "DocuMind – Quiz ôn tập", "Nộp bài", "↺ Làm lại", "Tài liệu làm quiz", "Câu 1. Q?", "✅ Đúng", "Workspace chưa có tài liệu.")]
    public async Task QuizWindowFollowsTheSelectedLanguage(bool vietnamese, string title, string submit, string retry,
        string documentsHeader, string questionTitle, string correct, string emptySummary)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                UiText.Current.IsVietnamese = vietnamese; // cũng đặt AppText.Vietnamese
                using var fixture = new SqliteFixture(); var providers = new FakeProviders();
                var rag = new RagOrchestratorService(providers, new FakeParser(), new EfDocumentStore(fixture));
                var vm = new QuizViewModel(new QuizService(rag, providers, new EfQuizStore(fixture)));
                var window = new QuizWindow(vm, SqliteFixture.WorkspaceId, "WS");
                var question = new QuizQuestionItem(1, new QuizQuestion("Q?", ["a", "b"], 0, "", null));
                vm.Questions.Add(question);
                window.Show(); window.UpdateLayout();

                Assert.Equal(title, window.Title);
                var buttons = Descendants(window).OfType<Button>().Select(b => b.Content as string).ToList();
                Assert.Contains(submit, buttons);
                Assert.Contains(retry, buttons);
                var texts = Descendants(window).OfType<TextBlock>().Select(t => t.Text).ToList();
                Assert.Contains(documentsHeader, texts);
                Assert.Contains(questionTitle, texts);
                Assert.Contains(texts, t => t.StartsWith("Workspace: WS"));

                Assert.StartsWith(emptySummary, vm.DocumentSelectionSummary); // chuỗi từ ViewModel (AppText) cũng đổi theo
                question.Options[0].IsSelected = true;
                question.Reveal();
                Assert.Equal(correct, question.ResultText);
                window.Close();
                completion.SetResult();
            }
            catch (Exception error) { completion.SetException(error); }
            finally { Dispatcher.CurrentDispatcher.InvokeShutdown(); }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(20));
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject owner)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(owner); i++)
        {
            var child = VisualTreeHelper.GetChild(owner, i);
            yield return child;
            foreach (var d in Descendants(child)) yield return d;
        }
    }

    public void Dispose()
    {
        AppText.Vietnamese = false;
        UiText.Current.IsVietnamese = false;
    }
}
