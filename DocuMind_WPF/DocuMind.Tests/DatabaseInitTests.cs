using DocuMind.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DocuMind.Tests;

public class DatabaseInitTests
{
    [Fact]
    public void EnsureCreated_WithSqlite_CreatesTablesAndSeedsDefaultWorkspace()
    {
        // Open an in-memory SQLite connection that persists for the duration of the test
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        using (var db = new AppDbContext(options))
        {
            db.Database.EnsureCreated().Should().BeTrue();

            var defaultWorkspace = db.Workspaces.FirstOrDefault(w => w.Id == Guid.Parse("11111111-1111-1111-1111-111111111111"));
            defaultWorkspace.Should().NotBeNull();
            defaultWorkspace!.Name.Should().Be("Workspace Mặc Định");
        }
    }

    [Fact]
    public async Task StartFreshConversationsAsync_RotatesOnlyWorkspacesWithMessagesAndPreservesHistory()
    {
        using var fixture = new SqliteFixture();
        await using var db = fixture.CreateDbContext();
        var workspace = await db.Workspaces.SingleAsync();
        var oldConversation = workspace.ConversationId;

        // No messages yet -> should not rotate
        Assert.Equal(0, await DatabaseMaintenance.StartFreshConversationsAsync(db));
        Assert.Equal(oldConversation, workspace.ConversationId);

        // Add messages to current conversation -> startup rotation must archive them and assign a new ConversationId
        db.ChatMessages.Add(new DocuMind.Domain.Entities.ChatMessage
        {
            WorkspaceId = workspace.Id,
            ConversationId = oldConversation,
            Content = "Saved question",
            Timestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var rotated = await DatabaseMaintenance.StartFreshConversationsAsync(db);
        Assert.Equal(1, rotated);

        var reloaded = await db.Workspaces.AsNoTracking().SingleAsync();
        Assert.NotEqual(oldConversation, reloaded.ConversationId);
        Assert.Single(await db.ChatMessages.Where(m => m.ConversationId == oldConversation).ToListAsync());
    }
}

