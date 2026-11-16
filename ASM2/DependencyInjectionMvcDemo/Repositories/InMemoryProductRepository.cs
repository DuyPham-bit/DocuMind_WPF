using DependencyInjectionMvcDemo.Interfaces;
using DependencyInjectionMvcDemo.Models;

namespace DependencyInjectionMvcDemo.Repositories;

/// <summary>
/// Repository lưu dữ liệu trong bộ nhớ (RAM) để chạy demo ngay không cần SQL Server.
/// Được đăng ký là Singleton trong Program.cs nên sử dụng Lock để đảm bảo thread-safe
/// khi nhiều HTTP request cùng truy cập đồng thời (Slide 9, 11, 18).
/// </summary>
public sealed class InMemoryProductRepository : IProductRepository
{
    private readonly Lock _syncRoot = new();
    private readonly List<Product> _products =
    [
        new Product
        {
            Id = 1,
            Name = "Chuột gaming",
            Description = "Cảm biến quang 16.000 DPI, 6 nút lập trình",
            Price = 250_000m,
            Stock = 12
        },
        new Product
        {
            Id = 2,
            Name = "Bàn phím cơ",
            Description = "Layout TKL 87 phím, switch cơ học gõ êm",
            Price = 590_000m,
            Stock = 7
        },
        new Product
        {
            Id = 3,
            Name = "Tai nghe USB",
            Description = "Micro lọc ồn, âm thanh stereo rõ nét cho học online",
            Price = 320_000m,
            Stock = 15
        }
    ];

    private int _nextId = 4;

    public IReadOnlyList<Product> GetAll()
    {
        lock (_syncRoot)
        {
            return _products
                .OrderBy(p => p.Id)
                .Select(Clone)
                .ToList();
        }
    }

    public Product? GetById(int id)
    {
        lock (_syncRoot)
        {
            var found = _products.FirstOrDefault(p => p.Id == id);
            return found is null ? null : Clone(found);
        }
    }

    public Product Add(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        lock (_syncRoot)
        {
            var created = Clone(product);
            created.Id = _nextId++;
            _products.Add(created);
            return Clone(created);
        }
    }

    public bool Update(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        lock (_syncRoot)
        {
            var existing = _products.FirstOrDefault(p => p.Id == product.Id);
            if (existing is null)
            {
                return false;
            }

            existing.Name = product.Name;
            existing.Description = product.Description;
            existing.Price = product.Price;
            existing.Stock = product.Stock;
            return true;
        }
    }

    public bool Delete(int id)
    {
        lock (_syncRoot)
        {
            var index = _products.FindIndex(p => p.Id == id);
            if (index < 0)
            {
                return false;
            }

            _products.RemoveAt(index);
            return true;
        }
    }

    private static Product Clone(Product source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Description = source.Description,
        Price = source.Price,
        Stock = source.Stock
    };
}
