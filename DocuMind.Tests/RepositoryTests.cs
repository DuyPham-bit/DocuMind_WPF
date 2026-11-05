using DocuMind.Domain.Entities;
using DocuMind.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DocuMind.Tests;

public class RepositoryTests
{
    private AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task DocumentChunkRepository_AddAndQueryByWorkspace_ReturnsOnlyWorkspaceChunks()
    {
        using var db = CreateInMemoryDbContext();
        var repo = new EfDocumentChunkRepository(db);

        var ws1 = Guid.NewGuid();
        var ws2 = Guid.NewGuid();

        await repo.AddRangeAsync(new[]
        {
            new DocumentChunk { WorkspaceId = ws1, FileName = "doc1.pdf", PageNumber = 1, Content = "A", EmbeddingJson = "[]" },
            new DocumentChunk { WorkspaceId = ws1, FileName = "doc1.pdf", PageNumber = 2, Content = "B", EmbeddingJson = "[]" },
            new DocumentChunk { WorkspaceId = ws2, FileName = "doc2.pdf", PageNumber = 1, Content = "C", EmbeddingJson = "[]" },
        });
        await repo.SaveChangesAsync();

        var ws1Chunks = await repo.GetByWorkspaceAsync(ws1);
        ws1Chunks.Should().HaveCount(2);
        ws1Chunks.Select(c => c.Content).Should().Contain(["A", "B"]);

        var distinctFiles = await repo.GetDistinctFilesByWorkspaceAsync(ws1);
        distinctFiles.Should().ContainSingle().Which.Should().Be("doc1.pdf");
    }

    [Fact]
    public async Task DocumentChunkRepository_DeleteByFileName_RemovesAllMatchingChunks()
    {
        using var db = CreateInMemoryDbContext();
        var repo = new EfDocumentChunkRepository(db);

        var wsId = Guid.NewGuid();
        await repo.AddRangeAsync(new[]
        {
            new DocumentChunk { WorkspaceId = wsId, FileName = "target.pdf", PageNumber = 1, Content = "Content 1", EmbeddingJson = "[]" },
            new DocumentChunk { WorkspaceId = wsId, FileName = "target.pdf", PageNumber = 2, Content = "Content 2", EmbeddingJson = "[]" },
            new DocumentChunk { WorkspaceId = wsId, FileName = "keep.pdf", PageNumber = 1, Content = "Keep Content", EmbeddingJson = "[]" }
        });
        await repo.SaveChangesAsync();

        await repo.DeleteByFileNameAsync(wsId, "target.pdf");

        var remaining = await repo.GetByWorkspaceAsync(wsId);
        remaining.Should().HaveCount(1);
        remaining[0].FileName.Should().Be("keep.pdf");
    }
}

