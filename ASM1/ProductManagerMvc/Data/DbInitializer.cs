using Microsoft.EntityFrameworkCore;
using ProductManagerMvc.Models;

namespace ProductManagerMvc.Data;

/// <summary>
/// Áp dụng migration và khởi tạo dữ liệu mẫu ban đầu khi bảng Products đang trống.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        if (await db.Products.AnyAsync())
        {
            return;
        }

        db.Products.AddRange(
            new Product
            {
                Name = "Chuột gaming",
                Description = "Chuột quang không dây 16000 DPI, LED RGB",
                Price = 250000,
                Stock = 12
            },
            new Product
            {
                Name = "Bàn phím cơ",
                Description = "Layout TKL 87 phím, switch Blue gõ êm",
                Price = 590000,
                Stock = 7
            },
            new Product
            {
                Name = "Tai nghe USB",
                Description = "Âm thanh vòm 7.1, micro lọc ồn cho học online",
                Price = 320000,
                Stock = 15
            }
        );

        await db.SaveChangesAsync();
    }
}
