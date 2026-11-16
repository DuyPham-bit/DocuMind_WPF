using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Documents;
using DocuMind.Infrastructure.Persistence;
using DocuMind.WPF;
using DocuMind.WPF.ViewModels;
using DocuMind.WPF.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DocuMind.Tests;

public class WpfSmokeTests
{
    [Fact]
    public async Task MainAndSettingsOpenAndClosingSettingsLeavesMainWindowOpen()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                using var fixture = new SqliteFixture();
                var providers = new FakeProviders();
                var mainVm = new MainViewModel(new(providers, new FakeParser(), new EfDocumentStore(fixture)), providers,
                    new EfRepository<DocuMind.Domain.Entities.Workspace>(fixture), new EfChatHistoryStore(fixture));
                using var services = new ServiceCollection().BuildServiceProvider();
                var main = new MainWindow(mainVm, services);
                main.Show();
                mainVm.ChatMessages.Add(new() { Role = DocuMind.Domain.Enums.MessageRole.Assistant, Content = "# Title\n**Bold** and `code`\n- Item\n```\nsafe code\n```" });
                main.UpdateLayout();
                mainVm.BusyStatus = "The AI provider returned HTTP 401. Check the key or model, then try again.";
                mainVm.IsBusy = false;
                main.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
                main.UpdateLayout();
                Assert.Contains(Descendants(main).OfType<TextBlock>(),
                    block => block.Text == mainVm.BusyStatus && block.IsVisible);
                var status = Descendants(main).OfType<TextBlock>().Single(block => block.Text == mainVm.BusyStatus && block.IsVisible);
                var statusBounds = status.TransformToAncestor(main).TransformBounds(new Rect(status.RenderSize));
                var chat = (ScrollViewer)main.FindName("ChatScrollViewer");
                var chatBounds = chat.TransformToAncestor(main).TransformBounds(new Rect(chat.RenderSize));
                Assert.True(statusBounds.Top >= chatBounds.Bottom, "Status must not cover the chat messages.");
                var markdown = Descendants(main).OfType<DocuMind.WPF.Controls.MarkdownView>().Single();
                var rendered = new TextRange(markdown.Document.ContentStart, markdown.Document.ContentEnd).Text;
                Assert.Contains("Title", rendered); Assert.Contains("Bold and code", rendered); Assert.Contains("safe code", rendered);
                var settingsVm = new SettingsViewModel(providers);
                var settings = new SettingsDialog(settingsVm) { Owner = main };
                settings.Show();
                settings.Width = settings.MinWidth;
                settings.UpdateLayout();
                var embeddingButton = Descendants(settings).OfType<Button>().Single(b => Equals(b.Content, "Test embedding"));
                var viewport = Descendants(settings).OfType<ScrollViewer>().First(v => v.Content is StackPanel);
                var bounds = embeddingButton.TransformToAncestor(viewport).TransformBounds(new Rect(embeddingButton.RenderSize));
                Assert.True(embeddingButton.ActualWidth >= 80,
                    $"Embedding test button must remain usable: width {embeddingButton.ActualWidth}.");
                Assert.True(bounds.Left >= 0 && bounds.Right <= viewport.ActualWidth,
                    $"Embedding test button must fit the viewport: {bounds}; width {viewport.ActualWidth}.");
                Assert.IsType<PasswordBox>(settings.FindName("KeyBox"));
                settings.Close();

                // Verify bilingual EN / VI toggle and Workspace display
                DocuMind.WPF.Localization.UiText.Current.IsVietnamese = false;
                Assert.Equal("My Workspace", new DocuMind.Domain.Entities.Workspace { Name = "My Workspace" }.ToString());
                DocuMind.WPF.Localization.UiText.Current.ToggleLanguageCommand.Execute(null);
                main.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
                main.UpdateLayout();
                Assert.Contains("Tóm tắt", mainVm.PromptTemplates.Select(t => t.Title));
                Assert.Contains(Descendants(main).OfType<Button>(), b => Equals(b.Content, "Xuất MD"));
                DocuMind.WPF.Localization.UiText.Current.ToggleLanguageCommand.Execute(null);
                Assert.Contains("Summarize", mainVm.PromptTemplates.Select(t => t.Title));

                Assert.True(main.IsVisible);
                main.Close();
                completion.SetResult();
            }
            catch (Exception error) { completion.SetException(error); }
            finally { Dispatcher.CurrentDispatcher.InvokeShutdown(); }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject owner)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(owner); i++)
        {
            var child = VisualTreeHelper.GetChild(owner, i);
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
}
