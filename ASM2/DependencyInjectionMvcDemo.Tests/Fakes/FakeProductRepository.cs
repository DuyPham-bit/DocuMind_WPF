using DependencyInjectionMvcDemo.Interfaces;
using DependencyInjectionMvcDemo.Models;

namespace DependencyInjectionMvcDemo.Tests.Fakes;

/// <summary>
/// Fake repository dùng riêng trong Unit Test để cô lập ProductService,
/// không cần HTTP server hay database thật (Slide 15).
/// </summary>
public sealed class FakeProductRepository : IProductRepository
{
    private readonly List<Product> _items;
    private int _nextId;

    public FakeProductRepository(IEnumerable<Product>? seed = null)
    {
        _items = seed?.Select(Clone).ToList() ?? [];
        _nextId = _items.Count == 0 ? 1 : _items.Max(p => p.Id) + 1;
    }

    public IReadOnlyList<Product> GetAll() => _items.Select(Clone).ToList();

    public Product? GetById(int id)
    {
        var item = _items.FirstOrDefault(p => p.Id == id);
        return item is null ? null : Clone(item);
    }

    public Product Add(Product product)
    {
        var created = Clone(product);
        if (created.Id <= 0)
        {
            created.Id = _nextId++;
        }
        _items.Add(created);
        return Clone(created);
    }

    public bool Update(Product product)
    {
        var index = _items.FindIndex(p => p.Id == product.Id);
        if (index < 0)
        {
            return false;
        }

        _items[index] = Clone(product);
        return true;
    }

    public bool Delete(int id)
    {
        var index = _items.FindIndex(p => p.Id == id);
        if (index < 0)
        {
            return false;
        }

        _items.RemoveAt(index);
        return true;
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
