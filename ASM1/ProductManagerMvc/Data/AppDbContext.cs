using Microsoft.EntityFrameworkCore;
using ProductManagerMvc.Models;

namespace ProductManagerMvc.Data;

/// <summary>
/// Lớp cầu nối giữa Entity Framework Core và cơ sở dữ liệu SQL Server.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Ánh xạ lớp Product thành bảng Products trong SQL Server.
    /// </summary>
    public DbSet<Product> Products => Set<Product>();
}
