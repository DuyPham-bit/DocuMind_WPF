using DocuMind.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DocuMind.Infrastructure.Postgres;

// Used by `dotnet ef migrations add X --project DocuMind.Infrastructure.Postgres --startup-project DocuMind.Infrastructure.Postgres`.
// Generating migrations never connects; the placeholder is only needed by `dotnet ef database update`.
public sealed class PostgresDesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>();
        options.UsePostgres(DatabaseMaintenance.PostgresConnectionFromEnvironment() ?? "Host=localhost;Database=documind_design;Username=postgres");
        return new AppDbContext(options.Options);
    }
}
