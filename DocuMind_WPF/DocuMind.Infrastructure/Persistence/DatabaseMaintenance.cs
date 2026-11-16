using DocuMind.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DocuMind.Infrastructure.Persistence;

public static class DatabaseMaintenance
{
    public static string DefaultPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DocuMind", "documind_local.db");
    public static string ConnectionString(string path) => new SqliteConnectionStringBuilder { DataSource = path, ForeignKeys = true }.ToString();

    public static void Backup(string source, string destination)
    {
        if (!File.Exists(source)) throw new FileNotFoundException("Không tìm thấy DB nguồn.");
        if (string.Equals(Path.GetFullPath(source), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("DB nguồn và đích phải khác nhau.");
        using var from = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = source, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
        using var to = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = destination, Pooling = false }.ToString());
        from.Open(); to.Open(); from.BackupDatabase(to);
    }

    // Call only with the application closed and contexts disposed.
    public static void Restore(string backup, string destination)
    {
        SqliteConnection.ClearAllPools();
        Backup(backup, destination);
    }

    public static async Task<string?> InitializeAsync(AppDbContext db, string path, string? legacyPath = null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        if (!File.Exists(path) && legacyPath != null && File.Exists(legacyPath)) Backup(legacyPath, path);
        var assembly = db.GetService<IMigrationsAssembly>();
        var baseline = assembly.Migrations.Keys.First();
        var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
        if (pending.Count == 0)
        {
            await RecoverStagingAsync(db);
            return null;
        }
        string? backup = null;
        var hasTables = false;
        await db.Database.OpenConnectionAsync();
        try
        {
            using var tables = db.Database.GetDbConnection().CreateCommand();
            tables.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name NOT LIKE '__EF%'";
            hasTables = Convert.ToInt32(await tables.ExecuteScalarAsync()) != 0;
            if (hasTables)
            {
                backup = path + ".backup-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
                Backup(path, backup);
            }
            if (hasTables && pending.Contains(baseline))
            {
                // Adopt only the exact old EnsureCreated schema; never blindly stamp an unrelated DB.
                var oldModel = assembly.CreateMigration(assembly.Migrations[baseline], db.Database.ProviderName!).TargetModel;
                oldModel = db.GetService<IModelRuntimeInitializer>().Initialize(oldModel, designTime: true);
                foreach (var table in oldModel.GetRelationalModel().Tables)
                {
                    using var command = db.Database.GetDbConnection().CreateCommand();
                    command.CommandText = $"PRAGMA table_info(\"{table.Name}\")";
                    using var reader = await command.ExecuteReaderAsync();
                    var columns = new Dictionary<string, string>();
                    while (await reader.ReadAsync()) columns.Add(reader.GetString(1), reader.GetString(2));
                    if (columns.Count != table.Columns.Count() || table.Columns.Any(c => !columns.TryGetValue(c.Name, out var type) || type != c.StoreType))
                        throw new InvalidOperationException("Schema DB cũ không được hỗ trợ. Backup đã được tạo; hãy khôi phục hoặc kiểm tra schema.");
                }
                await db.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS \"__EFMigrationsHistory\" (\"MigrationId\" TEXT NOT NULL CONSTRAINT \"PK___EFMigrationsHistory\" PRIMARY KEY, \"ProductVersion\" TEXT NOT NULL)");
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({baseline}, {"10.0.14"})");
            }
        }
        finally { await db.Database.CloseConnectionAsync(); }
        await db.Database.MigrateAsync();
        // A terminated ingest can never become queryable on restart.
        await RecoverStagingAsync(db);
        return backup;
    }

    private static Task<int> RecoverStagingAsync(AppDbContext db) => db.DocumentVersions.Where(v => v.Status == DocumentVersionStatus.Staging)
        .ExecuteUpdateAsync(s => s.SetProperty(v => v.Status, DocumentVersionStatus.Failed));
}
