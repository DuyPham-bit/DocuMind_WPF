using System.IO;
using System.Runtime.Versioning;
using System.Windows;
using DocuMind.Application;
using DocuMind.Application.Common;
using DocuMind.Infrastructure;
using DocuMind.WPF.ViewModels;
using DocuMind.WPF.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DocuMind.WPF;

[SupportedOSPlatform("windows")]
public partial class App : System.Windows.Application
{
    public IServiceProvider Services { get; }

    public App()
    {
        var services = new ServiceCollection();

        // Composition root: each layer registers its own services.
        services.AddApplication();
        services.AddInfrastructure();

        // Presentation: ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<QuizViewModel>();

        // Presentation: Views
        services.AddTransient<MainWindow>();
        services.AddTransient<SettingsDialog>();

        Services = services.BuildServiceProvider();
    }

    private Mutex? _instanceMutex;
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            DocuMind.WPF.Localization.UiText.Current.EnablePersistence(DocuMind.WPF.Localization.UiText.DefaultSettingsPath);
            _instanceMutex = new Mutex(true, "Local\\DocuMind-" + Environment.UserName, out var ownsInstance);
            if (!ownsInstance) { MessageBox.Show(AppText.T("DocuMind is already running in another window.", "DocuMind đang chạy ở một cửa sổ khác.")); Shutdown(); return; }
            using (var scope = Services.CreateScope())
                await scope.ServiceProvider.InitializeDatabaseAsync(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "documind_local.db"));

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(AppText.T($"The application could not start: {UserErrors.Describe(ex)}\nDatabase and backups: {InfrastructureModule.DataDirectory}",
                $"Không khởi động được ứng dụng: {UserErrors.Describe(ex)}\nDatabase và bản sao lưu: {InfrastructureModule.DataDirectory}"),
                AppText.T("DocuMind — Startup error", "DocuMind — Lỗi khởi động"), MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
    protected override void OnExit(ExitEventArgs e)
    {
        (Services as IDisposable)?.Dispose();
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }
}
