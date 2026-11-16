using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Input;
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
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(viewModel.ApiKey) && KeyBox.Password != viewModel.ApiKey)
                KeyBox.Password = viewModel.ApiKey;
            RevealedKey.Text = "";
            RevealedKey.Visibility = Visibility.Collapsed;
        };
        Closed += (_, _) => { viewModel.Dispose(); KeyBox.Clear(); };
        Deactivated += (_, _) => HideKey();

        Loaded += async (s, e) =>
        {
            await viewModel.InitializeAsync();
        };
    }

    private void KeyBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm) vm.ApiKey = KeyBox.Password;
    }
    private void RevealKey_Down(object sender, MouseButtonEventArgs e)
    {
        RevealedKey.Text = KeyBox.Password;
        RevealedKey.Visibility = Visibility.Visible;
    }
    private void HideKey() { RevealedKey.Text = ""; RevealedKey.Visibility = Visibility.Collapsed; }
    private void RevealKey_Up(object sender, MouseButtonEventArgs e) => HideKey();
    private void RevealKey_Leave(object sender, MouseEventArgs e) => HideKey();
}
