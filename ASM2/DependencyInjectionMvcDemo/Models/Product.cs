using System.ComponentModel.DataAnnotations;

namespace DependencyInjectionMvcDemo.Models;

/// <summary>
/// Đại diện cho một sản phẩm trong hệ thống demo Dependency Injection.
/// </summary>
public sealed class Product
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
    [StringLength(120, ErrorMessage = "Tên sản phẩm tối đa 120 ký tự.")]
    [Display(Name = "Tên sản phẩm")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự.")]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Range(1, 999_999_999, ErrorMessage = "Giá sản phẩm phải lớn hơn 0.")]
    [Display(Name = "Giá (VNĐ)")]
    public decimal Price { get; set; }

    [Range(0, 100_000, ErrorMessage = "Số lượng tồn kho không được âm.")]
    [Display(Name = "Tồn kho")]
    public int Stock { get; set; }
}
