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
                var markdown = Descendants(main).OfType<DocuMind.WPF.Controls.MarkdownView>().Single();
                var rendered = new TextRange(markdown.Document.ContentStart, markdown.Document.ContentEnd).Text;
                Assert.Contains("Title", rendered); Assert.Contains("Bold and code", rendered); Assert.Contains("safe code", rendered);
                var settingsVm = new SettingsViewModel(providers);
                var settings = new SettingsDialog(settingsVm) { Owner = main };
                settings.Show();
                Assert.IsType<PasswordBox>(settings.FindName("KeyBox"));
                settings.Close();
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
