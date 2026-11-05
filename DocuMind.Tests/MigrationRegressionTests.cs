using DocuMind.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DocuMind.Tests;

public class MigrationRegressionTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UpgradeLegacySchemaPreservesDataBacksUpAndCanRestore(bool ensureCreatedLegacy)
    {
        using var fixture = new SqliteFixture(false);
        await using (var db = fixture.CreateDbContext())
        {
            var baseline = db.GetService<IMigrationsAssembly>().Migrations.Keys.First();
            await db.GetService<IMigrator>().MigrateAsync(baseline);
            if (ensureCreatedLegacy) await db.Database.ExecuteSqlRawAsync("DROP TABLE __EFMigrationsHistory");
            await db.Database.ExecuteSqlRawAsync("""
                INSERT INTO DocumentChunks (Id, WorkspaceId, FileName, PageNumber, Content, EmbeddingJson, CreatedAt)
                VALUES ('33333333-3333-3333-3333-333333333333', '11111111-1111-1111-1111-111111111111', 'old.txt', 1, 'legacy content', '[1,0]', '2026-01-01');
                INSERT INTO DocumentChunks (Id, WorkspaceId, FileName, PageNumber, Content, EmbeddingJson, CreatedAt)
                VALUES ('44444444-4444-4444-4444-444444444444', '11111111-1111-1111-1111-111111111111', 'old.txt', 2, 'legacy second', '[1,0]', '2026-01-01');
                INSERT INTO ProviderSettings (ProviderType, EncryptedApiKey, BaseUrl, ChatModel, EmbeddingModel, Temperature, IsActive)
                VALUES (1, 'legacy-encrypted-bytes', '', 'legacy-chat', 'legacy-embed', 0.6, 1);
                INSERT INTO ChatMessages (Id, WorkspaceId, Role, Content, Timestamp) VALUES
                ('55555555-5555-5555-5555-555555555555', '11111111-1111-1111-1111-111111111111', 1, 'legacy question', '2026-01-01');
                """);
        }
        string? backup;
        await using (var db = fixture.CreateDbContext())
        {
            backup = await DatabaseMaintenance.InitializeAsync(db, fixture.DbPath);
            Assert.NotNull(backup); Assert.True(File.Exists(backup));
            Assert.Equal(2, await db.DocumentChunks.CountAsync()); Assert.Equal(1, await db.Documents.CountAsync());
            Assert.Equal("legacy-encrypted-bytes", (await db.ProviderSettings.SingleAsync()).EncryptedApiKey);
            Assert.False((await db.ProviderSettings.SingleAsync()).IsEmbeddingActive);
            Assert.Equal("legacy question", (await db.ChatMessages.SingleAsync()).Content);
            var workspace = await db.Workspaces.SingleAsync();
            Assert.Equal(workspace.ConversationId, (await db.ChatMessages.SingleAsync()).ConversationId);
            var providers = new FakeProviders(); var rag = new DocuMind.Application.Services.RagOrchestratorService(providers, new FakeParser(), new EfDocumentStore(fixture));
            Assert.Empty((await rag.ChatWithRagAsync(workspace.Id, "system", "question")).Sources);
            await rag.IngestDocumentAsync(workspace.Id, fixture.File("old.txt", "new content"));
            Assert.Equal(1, await db.Documents.CountAsync()); Assert.Equal(1, await db.DocumentChunks.CountAsync());
            Assert.Null(await DatabaseMaintenance.InitializeAsync(db, fixture.DbPath));
        }
        DatabaseMaintenance.Restore(backup!, fixture.DbPath);
        await using (var restored = fixture.CreateDbContext())
        {
            using var command = restored.Database.GetDbConnection().CreateCommand();
            await restored.Database.OpenConnectionAsync(); command.CommandText = "SELECT Content FROM DocumentChunks ORDER BY Id LIMIT 1";
            Assert.Equal("legacy content", await command.ExecuteScalarAsync());
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE name='Documents'";
            Assert.Equal(0L, await command.ExecuteScalarAsync());
        }
    }

    [Fact]
    public async Task ImportCopiesLegacyDatabaseWithoutChangingOriginal()
    {
        using var original = new SqliteFixture(); using var destination = new SqliteFixture(false);
        await using var db = destination.CreateDbContext();
        await DatabaseMaintenance.InitializeAsync(db, destination.DbPath, original.DbPath);
        Assert.Single(await db.Workspaces.ToListAsync()); Assert.True(File.Exists(original.DbPath));
    }

    [Fact]
    public async Task RestartMarksAbandonedStagingFailedEvenWithoutPendingMigrations()
    {
        using var fixture = new SqliteFixture(); var store = new EfDocumentStore(fixture);
        var staging = await store.BeginAsync(SqliteFixture.WorkspaceId, fixture.File(), default);
        await using var db = fixture.CreateDbContext();
        Assert.Null(await DatabaseMaintenance.InitializeAsync(db, fixture.DbPath));
        Assert.Equal(DocuMind.Domain.Entities.DocumentVersionStatus.Failed, (await db.DocumentVersions.SingleAsync(v => v.Id == staging.Id)).Status);
    }
}
