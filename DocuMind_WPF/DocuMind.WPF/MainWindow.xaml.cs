using System.Collections.Specialized;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Input;
using DocuMind.WPF.ViewModels;
using DocuMind.WPF.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DocuMind.WPF;

[SupportedOSPlatform("windows")]
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;
    private bool _closingArchiveCompleted;

    public MainWindow(MainViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        DataContext = _viewModel;

        _viewModel.OpenSettingsAction = ShowSettingsDialog;
        _viewModel.OpenQuizAction = ShowQuizWindow;

        // Auto scroll to bottom when new messages arrive
        ((INotifyCollectionChanged)_viewModel.ChatMessages).CollectionChanged += (s, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                Dispatcher.InvokeAsync(() =>
                {
                    ChatScrollViewer.ScrollToEnd();
                });
            }
        };

        Loaded += async (s, e) =>
        {
            await _viewModel.InitializeAsync();
        };

        Closing += async (_, e) =>
        {
            if (_closingArchiveCompleted || (!_viewModel.HasConversationActivity && _viewModel.ChatMessages.Count == 0))
            {
                return;
            }

            e.Cancel = true;
            try { await _viewModel.StartNewConversationOnCloseAsync(); }
            finally
            {
                _closingArchiveCompleted = true;
                Close();
            }
        };
    }

    private void ShowSettingsDialog()
    {
        var settingsVm = _serviceProvider.GetRequiredService<SettingsViewModel>();
        var dialog = new SettingsDialog(settingsVm)
        {
            Owner = this
        };
        dialog.ShowDialog();

        // Refresh provider display and auto-ingest any pending documents after saving API config.
        _ = _viewModel.OnProviderSettingsClosedAsync(settingsVm.SavedSuccessfully);
    }

    private void ShowQuizWindow(DocuMind.Domain.Entities.Workspace workspace)
    {
        var quizVm = _serviceProvider.GetRequiredService<QuizViewModel>();
        new QuizWindow(quizVm, workspace.Id, workspace.Name) { Owner = this }.ShowDialog();
    }

    private void TxtInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && !Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            e.Handled = true;
            if (_viewModel.SendMessageCommand.CanExecute(null))
            {
                _viewModel.SendMessageCommand.Execute(null);
            }
        }
    }
}
