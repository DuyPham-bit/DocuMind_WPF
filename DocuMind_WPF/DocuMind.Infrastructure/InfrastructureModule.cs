using DocuMind.Application.Interfaces;
using DocuMind.Infrastructure.AiProviders;
using DocuMind.Infrastructure.DocumentParsers;
using DocuMind.Infrastructure.Export;
using DocuMind.Infrastructure.Persistence;
using DocuMind.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DocuMind.Infrastructure;

public static class InfrastructureModule
{
    /// <summary>Folder holding the local database and its backups.</summary>
    public static string DataDirectory => Path.GetDirectoryName(DatabaseMaintenance.DefaultPath)!;

    /// <summary>
    /// Registers every Application port implementation. Uses Supabase/Postgres when
    /// <see cref="DatabaseMaintenance.PostgresEnvVar"/> is set, otherwise the local SQLite database.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        var postgres = DatabaseMaintenance.PostgresConnectionFromEnvironment();
        services.AddDbContextFactory<AppDbContext>(options =>
        {
            if (postgres != null) options.UsePostgres(postgres);
            else options.UseSqlite(DatabaseMaintenance.ConnectionString(DatabaseMaintenance.DefaultPath));
        });

        services.AddSingleton(_ => new HttpClient { Timeout = TimeSpan.FromMinutes(10) });

        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IDocumentChunkRepository, EfDocumentChunkRepository>();
        services.AddScoped<IDocumentParser, PdfPigDocumentParser>();
        services.AddSingleton<ISecretProtector, WindowsDpapiSecretProtector>();
        services.AddSingleton<IDocumentStore, EfDocumentStore>();
        services.AddSingleton<IChatHistoryStore, EfChatHistoryStore>();
        services.AddSingleton<IQuizStore, EfQuizStore>();
        services.AddSingleton<IChatPdfExporter, PdfPigConversationExporter>();
        services.AddScoped<IChatProviderFactory, ChatProviderFactory>();
        return services;
    }

    /// <summary>Migrates (and for SQLite backs up / adopts a legacy file), recovers staging rows and closes old conversations.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, string? legacySqlitePath = null)
    {
        var factory = services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var db = await factory.CreateDbContextAsync();
        await DatabaseMaintenance.InitializeAsync(db, DatabaseMaintenance.DefaultPath, legacySqlitePath);
        await DatabaseMaintenance.StartFreshConversationsAsync(db);
    }
}
