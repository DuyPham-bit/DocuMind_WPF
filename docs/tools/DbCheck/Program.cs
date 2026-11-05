using DocuMind.Domain.Entities;
using DocuMind.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

if (args.Length == 3 && args[0] is "backup" or "restore")
{
    var source = Path.GetFullPath(args[1]);
    var destination = Path.GetFullPath(args[2]);
    if (source.Equals(destination, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Source and destination must differ.");
    if (!File.Exists(source)) throw new FileNotFoundException("Source database not found.");
    if (args[0] == "backup")
    {
        if (File.Exists(destination)) throw new InvalidOperationException("Backup destination already exists. Choose a new file.");
        DatabaseMaintenance.Backup(source, destination);
    }
    else
    {
        if (File.Exists(destination))
        {
            var safetyCopy = destination + ".backup-before-restore-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            DatabaseMaintenance.Backup(destination, safetyCopy);
            Console.WriteLine("Saved current database to " + safetyCopy);
        }
        DatabaseMaintenance.Restore(source, destination);
    }
    Console.WriteLine(args[0] + " complete: " + destination);
    return;
}

if (args.Length > 1 || args.Length == 1 && args[0] != "sqlite")
    throw new ArgumentException("Usage: sqlite | backup <source> <new-destination> | restore <backup> <destination>. Close DocuMind before maintenance. SQL Server scripts describe the historical version.");

// Never opens the application's real DB. Verify the generated migration script on a private SQLite database.
var docsDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
using var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
connection.Open();
using (var script = connection.CreateCommand())
{
    script.CommandText = File.ReadAllText(Path.Combine(docsDirectory, "Database_SQLite.txt"));
    script.ExecuteNonQuery();
}
using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
foreach (var entity in db.Model.GetEntityTypes())
{
    var table = entity.GetTableName()!;
    var tableId = StoreObjectIdentifier.Table(table, entity.GetSchema());
    var columns = new Dictionary<string, (string Type, bool Nullable)>();
    using (var metadata = connection.CreateCommand())
    {
        metadata.CommandText = $"PRAGMA table_info(\"{table}\")";
        using var reader = metadata.ExecuteReader();
        while (reader.Read()) columns.Add(reader.GetString(1), (reader.GetString(2), reader.GetInt64(3) == 0));
    }
    Require(columns.Count == entity.GetProperties().Count(), table + " columns");
    foreach (var property in entity.GetProperties())
    {
        var actual = columns[property.GetColumnName(tableId)!];
        Require(actual.Type == property.GetColumnType() && actual.Nullable == property.IsNullable, table + "." + property.Name);
    }
    foreach (var index in entity.GetIndexes())
    {
        using var check = connection.CreateCommand();
        check.CommandText = $"SELECT COUNT(*) FROM pragma_index_list('{table}') WHERE name='{index.GetDatabaseName()}' AND \"unique\"={(index.IsUnique ? 1 : 0)}";
        Require(Convert.ToInt32(check.ExecuteScalar()) == 1, "index " + index.GetDatabaseName());
    }
    foreach (var foreign in entity.GetForeignKeys())
    {
        using var check = connection.CreateCommand();
        var action = foreign.DeleteBehavior == DeleteBehavior.Cascade ? "CASCADE" : "RESTRICT";
        check.CommandText = $"SELECT COUNT(*) FROM pragma_foreign_key_list('{table}') WHERE \"table\"='{foreign.PrincipalEntityType.GetTableName()}' AND on_delete='{action}'";
        Require(Convert.ToInt32(check.ExecuteScalar()) >= 1, "foreign key " + table);
    }
}
Require(db.Workspaces.Single().Name == "Workspace Mặc Định", "Unicode seed");
Require(!db.Database.GetPendingMigrations().Any(), "all migrations applied");
using (var transaction = db.Database.BeginTransaction())
{
    var workspace = new Workspace { Name = "Kiểm thử cascade" };
    db.Workspaces.Add(workspace);
    var document = new Document { WorkspaceId = workspace.Id, DisplayName = "sample.txt", SourcePath = "test:sample.txt" };
    db.Documents.Add(document);
    var version = new DocumentVersion { DocumentId = document.Id };
    db.DocumentVersions.Add(version);
    db.DocumentChunks.Add(new() { WorkspaceId = workspace.Id, DocumentVersionId = version.Id, FileName = "sample.txt", Content = "Tiếng Việt", EmbeddingJson = "[1,0]" });
    db.ChatMessages.Add(new() { WorkspaceId = workspace.Id, Content = "Câu hỏi" });
    db.SaveChanges();
    db.Workspaces.Where(w => w.Id == workspace.Id).ExecuteDelete();
    Require(!db.Documents.Any(d => d.WorkspaceId == workspace.Id) && !db.DocumentVersions.Any(v => v.DocumentId == document.Id) &&
        !db.DocumentChunks.Any(c => c.WorkspaceId == workspace.Id) && !db.ChatMessages.Any(m => m.WorkspaceId == workspace.Id), "cascade");
    transaction.Rollback();
}
using (var foreignKeys = connection.CreateCommand())
{
    foreignKeys.CommandText = "PRAGMA foreign_key_check";
    using var reader = foreignKeys.ExecuteReader();
    Require(!reader.Read(), "foreign_key_check");
}
Console.WriteLine("SQLite PASS: generated migrations, schema, indexes, FK, Unicode seed, rollback, cascade. Real app database was not opened.");

static void Require(bool value, string description)
{
    if (!value) throw new InvalidOperationException("Verification failed: " + description);
}
