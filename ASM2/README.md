# ASM2 — DependencyInjectionMvcDemo (Dependency Injection trong C# / .NET 10)

Dự án mẫu minh họa **Dependency Injection (DI)**, **Constructor Injection**, **Service Lifetimes** (`Transient`, `Scoped`, `Singleton`) và **Unit Test với Fake Repository** bằng **ASP.NET Core MVC (.NET 10)**, bám sát nội dung slide `Assignment2_DI_PRN212_Aligned_v3.pptx`.

- Xem hướng dẫn đầy đủ từ lý thuyết DI, Setup từng bước, giải thích Code mẫu đến kịch bản Demo tại: **[USER_GUIDE.md](./USER_GUIDE.md)**

## Chạy nhanh

```powershell
cd ASM2   # từ thư mục gốc của repo
dotnet test
dotnet run --project DependencyInjectionMvcDemo
```

Sau khi chạy, mở trình duyệt tại địa chỉ localhost hiển thị trên terminal:
- `/Products` — Danh sách & CRUD sản phẩm qua luồng `ProductsController -> IProductService -> IProductRepository`
- `/Lifetime` — Bảng so sánh GUID của `Transient`, `Scoped`, `Singleton` tại 2 điểm inject trong cùng HTTP request
