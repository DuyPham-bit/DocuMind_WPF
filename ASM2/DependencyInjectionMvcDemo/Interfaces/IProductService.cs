using DependencyInjectionMvcDemo.Models;

namespace DependencyInjectionMvcDemo.Interfaces;

/// <summary>
/// Contract tầng nghiệp vụ (Service): Controller chỉ phụ thuộc vào interface này
/// thay vì gọi trực tiếp Repository hay khởi tạo bằng từ khóa new.
/// </summary>
public interface IProductService
{
    IReadOnlyList<Product> GetAll(string? keyword = null);
    Product? GetById(int id);
    Product Create(Product product);
    bool Update(Product product);
    bool Delete(int id);
}
