using DependencyInjectionMvcDemo.Models;

namespace DependencyInjectionMvcDemo.Interfaces;

/// <summary>
/// Contract tầng dữ liệu (Repository): trừu tượng hóa cách lưu trữ sản phẩm
/// (In-Memory, SQL Server/EF Core, hoặc Fake khi viết Unit Test).
/// </summary>
public interface IProductRepository
{
    IReadOnlyList<Product> GetAll();
    Product? GetById(int id);
    Product Add(Product product);
    bool Update(Product product);
    bool Delete(int id);
}
