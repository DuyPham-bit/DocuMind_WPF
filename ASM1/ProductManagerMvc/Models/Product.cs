using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductManagerMvc.Models;

/// <summary>
/// Đại diện cho một sản phẩm trong hệ thống quản lý bán hàng (Model trong mô hình MVC).
/// </summary>
public class Product
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
    [StringLength(120, ErrorMessage = "Tên sản phẩm tối đa 120 ký tự.")]
    [Display(Name = "Tên sản phẩm")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự.")]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giá sản phẩm.")]
    [Range(1, 999999999, ErrorMessage = "Giá sản phẩm phải từ 1 đến 999.999.999 VNĐ.")]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Giá (VNĐ)")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số lượng tồn kho.")]
    [Range(0, 100000, ErrorMessage = "Số lượng tồn kho phải từ 0 đến 100.000.")]
    [Display(Name = "Tồn kho")]
    public int Stock { get; set; }
}
