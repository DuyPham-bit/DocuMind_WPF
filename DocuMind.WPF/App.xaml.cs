using System.IO;
using System.Net.Http;
using System.Runtime.Versioning;
using System.Windows;
using DocuMind.Application.Interfaces;
using DocuMind.Application.Services;
using DocuMind.Domain.Entities;
using DocuMind.Infrastructure.AiProviders;
using DocuMind.Infrastructure.DocumentParsers;
using DocuMind.Infrastructure.Persistence;
using DocuMind.Infrastructure.Security;
using DocuMind.WPF.ViewModels;
using DocuMind.WPF.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DocuMind.WPF;

[SupportedOSPlatform("windows")]
public partial class App : System.Windows.Application
{
    public IServiceProvider Services { get; }

    public App()
    {
        var services = new ServiceCollection();

        // 1. Database SQLite tự động lưu file cục bộ tại thư mục ứng dụng
        string dbPath = DatabaseMaintenance.DefaultPath;
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlite(DatabaseMaintenance.ConnectionString(dbPath)));

        // 2. HttpClient dùng chung
        services.AddSingleton<HttpClient>();

        // 3. Infrastructure Repositories & Parsers
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IDocumentChunkRepository, EfDocumentChunkRepository>();
        services.AddScoped<IDocumentParser, PdfPigDocumentParser>();
        services.AddSingleton<ISecretProtector, WindowsDpapiSecretProtector>();
        services.AddSingleton<IDocumentStore, EfDocumentStore>();
        services.AddSingleton<IChatHistoryStore, EfChatHistoryStore>();

        // 4. AI Provider Factory & Orchestrator
        services.AddScoped<IChatProviderFactory, ChatProviderFactory>();
        services.AddScoped<RagOrchestratorService>();

        // 5. ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<SettingsViewModel>();

        // 6. Views
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
            _instanceMutex = new Mutex(true, "Local\\DocuMind-" + Environment.UserName, out var ownsInstance);
            if (!ownsInstance) { MessageBox.Show("DocuMind đang chạy trong một cửa sổ khác."); Shutdown(); return; }
            using (var scope = Services.CreateScope())
            {
                var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
                await using var db = await factory.CreateDbContextAsync();
                await DatabaseMaintenance.InitializeAsync(db, DatabaseMaintenance.DefaultPath,
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "documind_local.db"));
            }

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khởi động ứng dụng: {UserErrors.Describe(ex)}\nDB/backup: {Path.GetDirectoryName(DatabaseMaintenance.DefaultPath)}",
                "DocuMind - Lỗi Khởi Động", MessageBoxButton.OK, MessageBoxImage.Error);
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
