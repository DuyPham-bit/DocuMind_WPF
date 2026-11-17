using DocuMind.Domain.Entities;
using DocuMind.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;

namespace DocuMind.Tests;

public class PostgresConfigTests
{
    private static AppDbContext PostgresContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>();
        options.UsePostgres("Host=localhost;Database=unused;Username=postgres");
        return new AppDbContext(options.Options);
    }

    [Fact]
    public void SupabaseUriIsConvertedToNpgsqlConnectionString()
    {
        var cs = DatabaseMaintenance.NormalizePostgres("postgresql://postgres.abc123:p%40ss%3Aword@aws-0-ap-southeast-1.pooler.supabase.com:5432/postgres");
        var b = new NpgsqlConnectionStringBuilder(cs);
        b.Host.Should().Be("aws-0-ap-southeast-1.pooler.supabase.com");
        b.Port.Should().Be(5432);
        b.Database.Should().Be("postgres");
        b.Username.Should().Be("postgres.abc123");
        b.Password.Should().Be("p@ss:word");
        b.SslMode.Should().Be(SslMode.Require);
    }

    [Fact]
    public void KeyValueConnectionStringIsKeptAsIs()
    {
        DatabaseMaintenance.NormalizePostgres(" Host=db;Username=u ").Should().Be("Host=db;Username=u");
    }

    [Fact]
    public void PostgresModelUsesPostgresSpecificSql()
    {
        using var db = PostgresContext();
        var model = db.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model;
        model.FindEntityType(typeof(Document))!.FindProperty(nameof(Document.SourcePath))!.GetCollation().Should().Be("case_insensitive");
        model.FindEntityType(typeof(ProviderSetting))!.GetIndexes().Select(i => i.GetFilter()).Should().Contain("\"IsActive\" = TRUE");
    }

    [Fact]
    public void PostgresMigrationsMatchTheModel()
    {
        using var db = PostgresContext();
        db.Database.GetMigrations().Should().NotBeEmpty();
        db.Database.HasPendingModelChanges().Should().BeFalse("run `dotnet ef migrations add` for DocuMind.Infrastructure.Postgres after model changes");
    }
}
