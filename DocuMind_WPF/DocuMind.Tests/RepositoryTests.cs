using DocuMind.Domain.Entities;
using DocuMind.Infrastructure.Persistence;

namespace DocuMind.Tests;

public class RepositoryTests
{
    [Fact]
    public async Task CommandsUseIndependentSqliteContextsAndDoNotLeakWorkspaceData()
    {
        using var fixture = new SqliteFixture();
        var repo = new EfRepository<Workspace>(fixture);
        var other = new Workspace { Name = "other" };
        await repo.AddAsync(other);
        var chunks = new EfDocumentChunkRepository(fixture);
        await chunks.AddRangeAsync([
            new() { WorkspaceId = SqliteFixture.WorkspaceId, FileName = "target.pdf", Content = "A", EmbeddingJson = "[]" },
            new() { WorkspaceId = SqliteFixture.WorkspaceId, FileName = "keep.pdf", Content = "B", EmbeddingJson = "[]" },
            new() { WorkspaceId = other.Id, FileName = "target.pdf", Content = "C", EmbeddingJson = "[]" }
        ]);
        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => repo.GetAllAsync()));
        await chunks.DeleteByFileNameAsync(SqliteFixture.WorkspaceId, "target.pdf");
        Assert.Single(await chunks.GetByWorkspaceAsync(SqliteFixture.WorkspaceId));
        Assert.Single(await chunks.GetByWorkspaceAsync(other.Id));
        Assert.True(fixture.ContextCount >= 25);
        await repo.DeleteAsync(other);
        Assert.Empty(await chunks.GetByWorkspaceAsync(other.Id));
    }
}
