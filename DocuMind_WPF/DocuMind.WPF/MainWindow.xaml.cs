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

    public MainWindow(MainViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        DataContext = _viewModel;

        _viewModel.OpenSettingsAction = ShowSettingsDialog;

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
    }

    private void ShowSettingsDialog()
    {
        var settingsVm = _serviceProvider.GetRequiredService<SettingsViewModel>();
        var dialog = new SettingsDialog(settingsVm)
        {
            Owner = this
        };
        dialog.ShowDialog();

        // Refresh provider display after settings dialog closes
        _ = _viewModel.RefreshActiveProviderInfoAsync();
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
