using System.Data.Common;
using System.Text.RegularExpressions;
using DocuMind.Domain.Entities;
using DocuMind.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

// Documentation-only utility. It never opens the application's SQLite file.
var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
var qa = Path.Combine(root, ".qa");
Directory.CreateDirectory(qa);
var server = args.Contains("sqlserver");
var schema = args.Contains("schema");
if (schema)
{
    var options = new DbContextOptionsBuilder<AppDbContext>();
    if (server) options.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=unused;Integrated Security=true");
    else options.UseSqlite("Data Source=:memory:");
    using var db = new AppDbContext(options.Options);
    var path = Path.Combine(qa, server ? "ef-sqlserver.sql" : "ef-sqlite.sql");
    File.WriteAllText(path, db.Database.GenerateCreateScript());
    Console.WriteLine(path);
    return;
}

if (!server)
{
    using var conn = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
    conn.Open();
    var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
    using var db = new AppDbContext(options);
    var text = File.ReadAllText(Path.Combine(root, "Database_SQLite.txt"));
    Execute(conn, text);
    Execute(conn, text);
    CheckModel(conn, db, false);
    CheckData(db);
    CheckBehavior(db);
    using var fk = conn.CreateCommand();
    fk.CommandText = "PRAGMA foreign_key_check";
    using var reader = fk.ExecuteReader();
    Require(!reader.Read(), "SQLite foreign_key_check");
    Console.WriteLine("SQLite PASS: schema, indexes, foreign keys, seed, Unicode, rerun, CRUD rollback, cascade.");
}
else
{
    // Only the uniquely named database created here can be dropped by this tool.
    var name = "DocuMind_DocsCheck_" + Guid.NewGuid().ToString("N");
    Require(Regex.IsMatch(name, "^DocuMind_DocsCheck_[0-9a-f]{32}$"), "Safe DB name");
    const string baseCs = "Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true;";
    using var master = new SqlConnection(baseCs + "Database=master;");
    master.Open();
    Execute(master, $"CREATE DATABASE [{name}]");
    try
    {
        using var conn = new SqlConnection(baseCs + $"Database={name};");
        conn.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(conn).Options;
        using var db = new AppDbContext(options);
        var text = File.ReadAllText(Path.Combine(root, "Database_SQLServer.txt"));
        foreach (var _ in Enumerable.Range(0, 2))
            foreach (var batch in Regex.Split(text, @"^\s*GO\s*\r?$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
                if (!string.IsNullOrWhiteSpace(batch)) Execute(conn, batch);
        CheckModel(conn, db, true);
        CheckData(db);
        CheckBehavior(db);
        Console.WriteLine("SQL Server PASS: schema, indexes, foreign keys, seed, Unicode, rerun, CRUD rollback, cascade.");
    }
    finally
    {
        SqlConnection.ClearAllPools();
        Execute(master, $"ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}]");
        Console.WriteLine("Removed temporary database " + name);
    }
}

static void Execute(DbConnection conn, string sql)
{
    using var cmd = conn.CreateCommand();
    cmd.CommandText = sql;
    cmd.ExecuteNonQuery();
}
static void Require(bool ok, string message)
{
    if (!ok) throw new InvalidOperationException("Verification failed: " + message);
}
static void CheckData(AppDbContext db)
{
    Require(db.Workspaces.Count() == 1, "one seed after rerun and optional rollback");
    var ws = db.Workspaces.Single();
    Require(ws.Id == Guid.Parse("11111111-1111-1111-1111-111111111111"), "seed ID");
    Require(ws.Name == "Workspace Mặc Định", "Unicode seed");
    Require(ws.SystemPrompt.Contains("chuẩn xác"), "seed prompt");
    Require(ws.CreatedAt == new DateTime(2026, 1, 1), "seed timestamp");
    Require(!db.ChatMessages.Any() && !db.DocumentChunks.Any() && !db.ProviderSettings.Any(), "optional examples rolled back");
}
static void CheckBehavior(AppDbContext db)
{
    using var tx = db.Database.BeginTransaction();
    var ws = new Workspace { Name = "Kiểm thử tiếng Việt", Description = "Ôn tập", SystemPrompt = "Chỉ trả lời theo tài liệu" };
    db.Workspaces.Add(ws);
    db.SaveChanges();
    db.DocumentChunks.Add(new DocumentChunk { WorkspaceId = ws.Id, FileName = "ôn tập.txt", PageNumber = 1, Content = "Chương trình học", EmbeddingJson = "[0.1,0.2]" });
    db.ChatMessages.Add(new ChatMessage { WorkspaceId = ws.Id, Content = "Câu hỏi tiếng Việt" });
    db.SaveChanges();
    db.ChangeTracker.Clear();
    Require(db.Workspaces.Single(x => x.Id == ws.Id).Name == "Kiểm thử tiếng Việt", "Unicode round trip");
    db.Database.ExecuteSqlInterpolated($"DELETE FROM Workspaces WHERE Id = {ws.Id}");
    Require(!db.DocumentChunks.Any(x => x.WorkspaceId == ws.Id) && !db.ChatMessages.Any(x => x.WorkspaceId == ws.Id), "DB cascade");
    tx.Rollback();
    db.ChangeTracker.Clear();
}
static void CheckModel(DbConnection conn, AppDbContext db, bool server)
{
    foreach (var entity in db.Model.GetEntityTypes())
    {
        var table = entity.GetTableName()!;
        var id = StoreObjectIdentifier.Table(table, entity.GetSchema());
        var actual = new Dictionary<string, (string type, bool nullable)>();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = server
                ? $"SELECT COLUMN_NAME, DATA_TYPE + CASE WHEN DATA_TYPE = 'nvarchar' THEN '(' + CASE WHEN CHARACTER_MAXIMUM_LENGTH = -1 THEN 'max' ELSE CAST(CHARACTER_MAXIMUM_LENGTH AS varchar(10)) END + ')' ELSE '' END, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='dbo' AND TABLE_NAME='{table}'"
                : $"PRAGMA table_info(\"{table}\")";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                if (server) actual[r.GetString(0)] = (r.GetString(1), r.GetString(2) == "YES");
                else actual[r.GetString(1)] = (r.GetString(2), r.GetInt64(3) == 0);
            }
        }
        Require(actual.Count == entity.GetProperties().Count(), table + " column count");
        foreach (var p in entity.GetProperties())
        {
            var col = p.GetColumnName(id)!;
            Require(actual.TryGetValue(col, out var found), table + "." + col);
            Require(string.Equals(found.type, p.GetColumnType(), StringComparison.OrdinalIgnoreCase), table + "." + col + " type " + found.type + " vs " + p.GetColumnType());
            Require(found.nullable == p.IsNullable, table + "." + col + " nullability");
        }
        foreach (var index in entity.GetIndexes())
        {
            var name = index.GetDatabaseName()!;
            using var cmd = conn.CreateCommand();
            cmd.CommandText = server
                ? $"SELECT COUNT(*) FROM sys.indexes WHERE name='{name}' AND object_id=OBJECT_ID('dbo.{table}') AND is_unique={(index.IsUnique ? 1 : 0)}"
                : $"SELECT COUNT(*) FROM pragma_index_list('{table}') WHERE name='{name}' AND \"unique\"={(index.IsUnique ? 1 : 0)}";
            Require(Convert.ToInt32(cmd.ExecuteScalar()) == 1, "index " + name);
        }
        foreach (var foreign in entity.GetForeignKeys())
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = server
                ? $"SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID('dbo.{table}') AND referenced_object_id=OBJECT_ID('dbo.{foreign.PrincipalEntityType.GetTableName()}') AND delete_referential_action_desc='CASCADE'"
                : $"SELECT COUNT(*) FROM pragma_foreign_key_list('{table}') WHERE \"table\"='{foreign.PrincipalEntityType.GetTableName()}' AND on_delete='CASCADE'";
            Require(Convert.ToInt32(cmd.ExecuteScalar()) == 1, "FK " + table);
        }
    }
}
