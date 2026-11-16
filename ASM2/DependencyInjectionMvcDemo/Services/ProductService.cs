using DependencyInjectionMvcDemo.Interfaces;
using DependencyInjectionMvcDemo.Models;

namespace DependencyInjectionMvcDemo.Services;

/// <summary>
/// Tầng nghiệp vụ (Business Logic): nhận IProductRepository qua Constructor Injection,
/// không tự tạo repository bằng từ khóa new (Slide 12).
/// </summary>
public sealed class ProductService : IProductService
{
    private readonly IProductRepository _repo;

    public ProductService(IProductRepository repo)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    }

    public IReadOnlyList<Product> GetAll(string? keyword = null)
    {
        var all = _repo.GetAll();
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return all;
        }

        var trimmed = keyword.Trim();
        return all
            .Where(p => p.Name.Contains(trimmed, StringComparison.OrdinalIgnoreCase)
                     || (!string.IsNullOrWhiteSpace(p.Description)
                         && p.Description.Contains(trimmed, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    public Product? GetById(int id)
    {
        if (id <= 0)
        {
            return null;
        }

        return _repo.GetById(id);
    }

    public Product Create(Product product)
    {
        var normalized = NormalizeAndValidate(product);
        return _repo.Add(normalized);
    }

    public bool Update(Product product)
    {
        var normalized = NormalizeAndValidate(product);
        return _repo.Update(normalized);
    }

    public bool Delete(int id)
    {
        if (id <= 0)
        {
            return false;
        }

        return _repo.Delete(id);
    }

    private static Product NormalizeAndValidate(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        var name = product.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Tên sản phẩm không được để trống.", nameof(product));
        }

        if (product.Price <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(product), "Giá sản phẩm phải lớn hơn 0.");
        }

        if (product.Stock < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(product), "Tồn kho không được âm.");
        }

        return new Product
        {
            Id = product.Id,
            Name = name,
            Description = string.IsNullOrWhiteSpace(product.Description) ? null : product.Description.Trim(),
            Price = product.Price,
            Stock = product.Stock
        };
    }
}
