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
}

