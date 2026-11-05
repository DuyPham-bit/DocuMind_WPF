using System.Runtime.Versioning;
using System.Windows;
using DocuMind.WPF.ViewModels;

namespace DocuMind.WPF.Views;

[SupportedOSPlatform("windows")]
public partial class SettingsDialog : Window
{
    public SettingsDialog(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseAction = Close;

        Loaded += async (s, e) =>
        {
            await viewModel.InitializeAsync();
        };
    }
}

