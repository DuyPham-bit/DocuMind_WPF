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
        string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "documind_local.db");
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // 2. HttpClient dùng chung
        services.AddSingleton<HttpClient>();

        // 3. Infrastructure Repositories & Parsers
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IDocumentChunkRepository, EfDocumentChunkRepository>();
        services.AddScoped<IDocumentParser, PdfPigDocumentParser>();

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

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            using (var scope = Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated(); // Tự động tạo bảng SQLite nếu chưa có
            }

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khởi động ứng dụng: {ex.Message}\n\nChi tiết: {ex.InnerException?.Message}", 
                "DocuMind - Lỗi Khởi Động", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}
