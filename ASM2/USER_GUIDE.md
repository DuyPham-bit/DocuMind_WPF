# USER GUIDE: DEPENDENCY INJECTION TRONG C#, SETUP & CODE MẪU (ASM2 - PRN212)

Tài liệu hướng dẫn từng bước dành cho người mới bắt đầu học **C# / .NET**, giúp bạn:
1. Hiểu **Dependency Injection (DI)** trong C# là gì, giải quyết vấn đề gì và DI Container hoạt động ra sao.
2. Tự tay **setup** và chạy project mẫu `DependencyInjectionMvcDemo` (.NET 10) ngay không cần cấu hình SQL Server.
3. Đọc hiểu trọn bộ **code mẫu** (`Interface -> Repository -> Service -> Controller -> Program.cs`), chứng minh 3 **Service Lifetimes** (`Transient`, `Scoped`, `Singleton`) bằng `Guid`, và viết **Unit Test** với `FakeProductRepository`.

> **Ranh giới kiến thức (theo định hướng Assignment 2 - PRN212):**
> - **Nền tảng PRN212 được vận dụng:** C#, OOP, `constructor`, `interface`, tư duy tách lớp UI/Logic/Data (như WPF) và `EF Core` / `DbContext`.
> - **Kiến thức mở rộng của Assignment 2:** **DI Container** (đăng ký & resolve tự động), **Service Lifetimes** (`Transient`, `Scoped`, `Singleton`), và dùng **ASP.NET Core MVC** làm môi trường quan sát vòng đời HTTP request.

---

## Mục lục
- [I. Dependency Injection trong C# thế nào?](#i-dependency-injection-trong-c-thế-nào)
  - [1. Vấn đề khi tự `new` dependency (Tight Coupling)](#1-vấn-đề-khi-tự-new-dependency-tight-coupling)
  - [2. DI và Constructor Injection giải quyết thế nào?](#2-di-và-constructor-injection-giải-quyết-thế-nào)
  - [3. Luồng DI Container resolve một dependency](#3-luồng-di-container-resolve-một-dependency)
- [II. User Guide: Setup & Chạy project mẫu `ASM2`](#ii-user-guide-setup--chạy-project-mẫu-asm2)
  - [1. Chuẩn bị môi trường](#1-chuẩn-bị-môi-trường)
  - [2. Chạy nhanh project có sẵn trong thư mục `ASM2`](#2-chạy-nhanh-project-có-sẵn-trong-thư-mục-asm2)
  - [3. Tự khởi tạo lại project từ đầu bằng lệnh CLI](#3-tự-khởi-tạo-lại-project-từ-đầu-bằng-lệnh-cli)
  - [4. Cấu trúc 4 khu vực trong `DependencyInjectionMvcDemo`](#4-cấu-trúc-4-khu-vực-trong-dependencyinjectionmvcdemo)
- [III. Giải thích Code mẫu theo quy trình 5 bước](#iii-giải-thích-code-mẫu-theo-quy-trình-5-bước)
  - [Bước 1 — Contract (`Interfaces/`)](#bước-1--contract-interfaces)
  - [Bước 2 — Implement (`Repositories/` và `Services/`)](#bước-2--implement-repositories-và-services)
  - [Bước 3 — Register tại Composition Root (`Program.cs`)](#bước-3--register-tại-composition-root-programcs)
  - [Bước 4 — Inject qua Constructor (`Controllers/`)](#bước-4--inject-qua-constructor-controllers)
  - [Bước 5 — Verify bằng Demo & Unit Test (`FakeProductRepository`)](#bước-5--verify-bằng-demo--unit-test-fakeproductrepository)
- [IV. 3 Service Lifetimes & Cách chọn đúng](#iv-3-service-lifetimes--cách-chọn-đúng)
  - [1. Phân biệt `Transient`, `Scoped`, `Singleton`](#1-phân-biệt-transient-scoped-singleton)
  - [2. Chứng minh bằng `Guid.NewGuid()` tại `/Lifetime`](#2-chứng-minh-bằng-guidnewguid-tại-lifetime)
  - [3. Mở rộng: Hoán đổi sang EF Core (`DbContext` là Scoped)](#3-mở-rộng-hoán-đổi-sang-ef-core-dbcontext-là-scoped)
- [V. Lỗi thường gặp, Checklist 4 chốt & Kịch bản Demo 5 phút](#v-lỗi-thường-gặp-checklist-4-chốt--kịch-bản-demo-5-phút)
- [VI. Tài liệu tham khảo chính thức (R1–R10)](#vi-tài-liệu-tham-khảo-chính-thức-r1r10)

---

## I. Dependency Injection trong C# thế nào?

### 1. Vấn đề khi tự `new` dependency (Tight Coupling)

Trong các bài C# cơ bản hoặc WPF, một lớp giao diện/controller thường tự khởi tạo lớp mình cần bằng từ khóa `new`:

```csharp
// Cách cũ: UI / Controller tự tạo cả chuỗi phụ thuộc
public class ProductsController : Controller
{
    private readonly ProductService _service =
        new ProductService(new InMemoryProductRepository());
}
```

Cách này **vẫn chạy**, nhưng có 3 nhược điểm lớn khi dự án lớn lên:
1. **Phụ thuộc chặt (Tight Coupling):** `ProductsController` biết quá rõ chi tiết bên trong (`ProductService` cần `InMemoryProductRepository`).
2. **Khó thay thế:** Nếu muốn đổi từ `InMemoryProductRepository` sang `EfCoreProductRepository` (SQL Server), bạn phải đi sửa code ở khắp các Controller hoặc cửa sổ UI.
3. **Khó Unit Test:** Khi viết test cho Controller hoặc Service, bạn không thể tách riêng logic nghiệp vụ ra khỏi dữ liệu thật/database.

---

### 2. DI và Constructor Injection giải quyết thế nào?

**Dependency Injection (DI)** là kỹ thuật đảo ngược quyền khởi tạo: **một class không tự tạo thứ mình cần (`new`), mà khai báo thứ mình cần qua constructor để bên ngoài (DI Container) truyền vào.**

```text
[UI / ProductsController]  ──yêu cầu──►  IProductService
                                               │
                                        (ProductService)
                                               │
                                           ──yêu cầu──►  IProductRepository
                                                               │
                                                  (InMemoryProductRepository)
```

So sánh trực diện (Slide 6):

```csharp
// KHÔNG DÙNG DI (tự tạo dependency)
private readonly ProductService _service
    = new ProductService(new InMemoryProductRepository());

// DÙNG CONSTRUCTOR INJECTION
private readonly IProductService _service;

public ProductsController(IProductService service)
{
    _service = service;
}
```

- `ProductsController` chỉ biết **contract** `IProductService`.
- `ProductService` chỉ biết **contract** `IProductRepository`.
- **Duy nhất `Program.cs`** (gọi là **Composition Root**) biết class thật nào đang được lắp vào.

> **Lưu ý:** DI **không bắt buộc** mọi class đều phải có interface; tuy nhiên interface rất hữu ích khi bạn cần thay đổi implementation (In-Memory <-> SQL Server) hoặc truyền Fake vào Unit Test.

---

### 3. Luồng DI Container resolve một dependency

Khi người dùng mở `/Products`, 5 bước sau diễn ra tự động (Slide 4):

```text
1. Program.cs        ──► Đăng ký mapping: IProductService -> ProductService, IProductRepository -> InMemoryProductRepository
2. DI Container      ──► Lưu bản đồ khởi tạo và quản lý vòng đời (Lifetime) của từng service
3. ProductsController──► Framework tạo Controller, thấy constructor cần IProductService
4. ProductService    ──► Container tạo ProductService, thấy constructor cần IProductRepository
5. Repository        ──► Container cấp InMemoryProductRepository, lắp ghép xong toàn chuỗi và trả Controller
```

---

## II. User Guide: Setup & Chạy project mẫu `ASM2`

### 1. Chuẩn bị môi trường

1. **.NET 10 SDK:** Kiểm tra trong PowerShell/Terminal:
   ```powershell
   dotnet --version
   ```
   (Ví dụ: `10.0.403`).
2. **IDE (chọn 1 trong 2):**
   - **Visual Studio 2022/2026** (có workload *ASP.NET and web development*) → mở `ASM2\ASM2.slnx` và nhấn `Ctrl + F5`.
   - **VS Code** + extension **C# Dev Kit** → mở thư mục `ASM2` và dùng terminal (`Ctrl + ~`).
3. **Database:** Project mẫu mặc định dùng `InMemoryProductRepository` (lưu trong RAM) nên **chạy ngay 100%, không cần cài hoặc bật SQL Server**.

---

### 2. Chạy nhanh project có sẵn trong thư mục `ASM2`

Mở PowerShell và chạy lần lượt các lệnh sau:

```powershell
# Bước 1: Di chuyển vào thư mục ASM2
cd d:\CN5.1\PRN212\26.FA.PRN212\Project-Prn212\ASM2

# Bước 2: Khôi phục packages & build kiểm tra lỗi biên dịch
dotnet restore
dotnet build

# Bước 3: Chạy toàn bộ Unit Tests (kiểm tra ProductService, InMemoryRepository, Lifetimes, Controllers)
dotnet test

# Bước 4: Khởi chạy ứng dụng web demo
dotnet run --project DependencyInjectionMvcDemo
```

Khi terminal in ra dòng kiểu `Now listening on: http://localhost:5xxx`, hãy mở trình duyệt truy cập:
- `http://localhost:5xxx/Products` — Xem 3 sản phẩm mẫu (`#1 Chuột gaming`, `#2 Bàn phím cơ`, `#3 Tai nghe USB`), thử tìm kiếm, thêm, xem, sửa, xóa.
- `http://localhost:5xxx/Lifetime` — Xem GUID của `Transient`, `Scoped`, `Singleton` và bấm **Refresh trang để kiểm chứng**.

---

### 3. Tự khởi tạo lại project từ đầu bằng lệnh CLI

Nếu bạn muốn tự tạo lại từ số 0 để luyện tập (Slide 7):

```powershell
# 1. Tạo solution và project MVC mới (.NET 10)
dotnet new sln -n ASM2 --format slnx
dotnet new mvc -n DependencyInjectionMvcDemo -f net10.0

# 2. Tạo project xUnit test và gắn vào solution
dotnet new xunit -n DependencyInjectionMvcDemo.Tests -f net10.0
dotnet sln ASM2.slnx add DependencyInjectionMvcDemo\DependencyInjectionMvcDemo.csproj DependencyInjectionMvcDemo.Tests\DependencyInjectionMvcDemo.Tests.csproj
dotnet add DependencyInjectionMvcDemo.Tests\DependencyInjectionMvcDemo.Tests.csproj reference DependencyInjectionMvcDemo\DependencyInjectionMvcDemo.csproj

# 3. Chạy thử
dotnet run --project DependencyInjectionMvcDemo
```

> **Lưu ý:** ASP.NET Core MVC tích hợp sẵn DI Container (`Microsoft.Extensions.DependencyInjection`), nên bạn **không cần cài thêm package NuGet DI riêng**.

---

### 4. Cấu trúc 4 khu vực trong `DependencyInjectionMvcDemo`

Theo Slide 8, khi đọc code DI bạn không cần đọc mọi file cùng lúc — hãy lần theo 1 dependency từ **Composition Root** đến **Business/Data** và **Presentation**:

```text
ASM2/
├── ASM2.slnx
├── DependencyInjectionMvcDemo/
│   ├── Program.cs                        # [1] COMPOSITION ROOT: Đăng ký interface <-> implementation + lifetime
│   ├── Interfaces/                       # [2] CONTRACTS: IProductRepository, IProductService, ILifetimeMarkers, ILifetimeSnapshotService
│   ├── Repositories/                     # [2] DATA ACCESS: InMemoryProductRepository (thread-safe Singleton)
│   ├── Services/                         # [2] BUSINESS LOGIC & MARKERS: ProductService, Transient/Scoped/SingletonMarker, LifetimeSnapshotService
│   ├── Models/                           # Product + LifetimeComparisonViewModel
│   ├── Controllers/                      # [3] PRESENTATION: ProductsController, LifetimeController (Constructor Injection)
│   └── Views/                            # [3] UI RAZOR: Products/ (Index, Details, Create, Edit, Delete) & Lifetime/Index.cshtml
└── DependencyInjectionMvcDemo.Tests/     # [4] VERIFICATION: xUnit tests với FakeProductRepository & ServiceProvider ValidateScopes
```

---

## III. Giải thích Code mẫu theo quy trình 5 bước

Quy trình áp dụng DI gồm đúng **5 thao tác** (Slide 17):
`01. CONTRACT` → `02. IMPLEMENT` → `03. REGISTER` → `04. INJECT` → `05. VERIFY`

### Bước 1 — Contract (`Interfaces/`)

File `Interfaces/IProductRepository.cs` định nghĩa các thao tác dữ liệu cần có, không quan tâm lưu ở RAM hay SQL Server:

```csharp
public interface IProductRepository
{
    IReadOnlyList<Product> GetAll();
    Product? GetById(int id);
    Product Add(Product product);
    bool Update(Product product);
    bool Delete(int id);
}
```

File `Interfaces/IProductService.cs` định nghĩa nghiệp vụ cho Controller sử dụng:

```csharp
public interface IProductService
{
    IReadOnlyList<Product> GetAll(string? keyword = null);
    Product? GetById(int id);
    Product Create(Product product);
    bool Update(Product product);
    bool Delete(int id);
}
```

---

### Bước 2 — Implement (`Repositories/` và `Services/`)

**1) `Repositories/InMemoryProductRepository.cs`:**
- Chứa sẵn 3 sản phẩm mẫu (`Chuột gaming`, `Bàn phím cơ`, `Tai nghe USB`).
- Vì được đăng ký là `Singleton` (nhiều HTTP request dùng chung 1 instance), lớp này dùng `Lock _syncRoot = new();` để đảm bảo **thread-safe** (tránh xung đột dữ liệu khi 2 request ghi cùng lúc) và trả về bản sao (`Clone`) để bên ngoài không vô tình sửa trực tiếp dữ liệu lưu trữ.

**2) `Services/ProductService.cs`:**
- Nhận `IProductRepository` qua **Constructor Injection**:

```csharp
public sealed class ProductService : IProductService
{
    private readonly IProductRepository _repo;

    public ProductService(IProductRepository repo)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    }
    ...
}
```

- `ProductService` xử lý logic nghiệp vụ (cắt khoảng trắng thừa, lọc tìm kiếm không phân biệt hoa thường, kiểm tra `Price > 0`, `Stock >= 0`) mà **không hề biết** `_repo` bên dưới là `InMemoryProductRepository`, `EfCoreProductRepository`, hay `FakeProductRepository`.

---

### Bước 3 — Register tại Composition Root (`Program.cs`)

Đây là nơi duy nhất lắp ghép interface với class cụ thể và chọn **Lifetime** (Slide 11):

```csharp
builder.Services.AddControllersWithViews();

// 1. Đăng ký tầng dữ liệu & nghiệp vụ
builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();

// 2. Đăng ký 3 marker và service đối chứng cho trang /Lifetime
builder.Services.AddTransient<ITransientMarker, TransientMarker>();
builder.Services.AddScoped<IScopedMarker, ScopedMarker>();
builder.Services.AddSingleton<ISingletonMarker, SingletonMarker>();
builder.Services.AddScoped<ILifetimeSnapshotService, LifetimeSnapshotService>();
```

> **Lưu ý quan trọng:** Không gọi `builder.Services.BuildServiceProvider()` thủ công bên trong `Program.cs` khi đang cấu hình dịch vụ, vì sẽ tạo ra bản sao Singleton thứ hai và gây rò rỉ tài nguyên.

---

### Bước 4 — Inject qua Constructor (`Controllers/`)

`Controllers/ProductsController.cs` khai báo rõ dependency qua constructor (Slide 13):

```csharp
public sealed class ProductsController : Controller
{
    private readonly IProductService _service;

    public ProductsController(IProductService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public IActionResult Index(string? q)
    {
        var products = _service.GetAll(q);
        return View(products);
    }
}
```

> Controller **không tự đăng ký** vào container theo mặc định, nhưng khi có request đến, ASP.NET Core MVC sẽ nhờ DI Container tự động truyền `IProductService` vào constructor của Controller.

---

### Bước 5 — Verify bằng Demo & Unit Test (`FakeProductRepository`)

Trong `DependencyInjectionMvcDemo.Tests/ProductServiceTests.cs`, ta kiểm thử `ProductService` hoàn toàn độc lập bằng `FakeProductRepository` (Slide 15):

```csharp
[Fact]
public void GetById_ReturnsMatchingProduct_FromInjectedFakeRepository()
{
    // Arrange: chuẩn bị dữ liệu giả
    var fakeRepo = new FakeProductRepository([
        new Product { Id = 42, Name = "Bàn phím cơ mini", Price = 750_000m, Stock = 5 }
    ]);
    var service = new ProductService(fakeRepo);

    // Act
    var result = service.GetById(42);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(42, result.Id);
    Assert.Equal("Bàn phím cơ mini", result.Name);
}
```

Không cần chạy web server, không cần SQL Server — test chạy chỉ vài mili-giây!

---

## IV. 3 Service Lifetimes & Cách chọn đúng

### 1. Phân biệt `Transient`, `Scoped`, `Singleton`

| Lifetime | Lệnh đăng ký | Khi nào tạo instance mới? | Dùng cho loại class nào? | Lưu ý an toàn |
| :--- | :--- | :--- | :--- | :--- |
| **Transient** | `AddTransient<TInterface, TImpl>()` | **Mỗi lần resolve / inject** đều tạo mới | Service nhẹ, không giữ trạng thái chung (stateless) | Nếu inject vào Scoped/Singleton, instance đó chỉ được tạo lúc cha nó được tạo — không tự mới mỗi lần gọi method |
| **Scoped** | `AddScoped<TInterface, TImpl>()` | **1 instance dùng chung trong cùng 1 scope** (trong web MVC là **1 HTTP request**) | Logic xử lý theo request, **EF Core `DbContext`** | Tuyệt đối **không inject Scoped vào Singleton** (lỗi *Captive Dependency*) |
| **Singleton** | `AddSingleton<TInterface, TImpl>()` | **1 instance duy nhất** từ lúc app khởi động đến khi tắt | Cấu hình dùng chung, cache, in-memory store | Bắt buộc phải **thread-safe** nếu có dữ liệu thay đổi; không giữ state riêng của từng user |

---

### 2. Chứng minh bằng `Guid.NewGuid()` tại `/Lifetime`

Để nhìn thấy sự khác biệt thật bằng mắt thay vì đoán lý thuyết (Slide 10):
- Cả 3 class `TransientMarker`, `ScopedMarker`, `SingletonMarker` đều chỉ làm một việc: tạo `public Guid Id { get; } = Guid.NewGuid();` ngay lúc container khởi tạo object.
- Trong cùng 1 HTTP request đến `/Lifetime`:
  - **Điểm inject 1:** `LifetimeController` nhận trực tiếp `ITransientMarker`, `IScopedMarker`, `ISingletonMarker`.
  - **Điểm inject 2:** `LifetimeSnapshotService` (cũng nằm trong cùng request đó) nhận lại 3 marker đó qua constructor của chính nó.

Kết quả quan sát được trên màn hình `/Lifetime`:
- **Transient:** `T1 ≠ T2` ngay trong cùng 1 request; bấm Refresh (F5) lại ra cặp GUID mới.
- **Scoped:** `S1 = S2` trong cùng 1 request; bấm Refresh (F5) đổi sang GUID mới (vì là HTTP request mới).
- **Singleton:** `G1 = G2` trong cùng 1 request; bấm Refresh (F5) **vẫn giữ nguyên GUID cũ**.

---

### 3. Mở rộng: Hoán đổi sang EF Core (`DbContext` là Scoped)

Nếu sau này bạn muốn lưu sản phẩm xuống **SQL Server** thay vì RAM:
1. Tạo `Repositories/EfCoreProductRepository.cs` implement `IProductRepository` và nhận `AppDbContext` qua constructor.
2. Tại `Program.cs`, chỉ cần đổi 1 dòng đăng ký từ `AddSingleton` sang `AddScoped`:
   ```csharp
   // builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();
   builder.Services.AddDbContext<AppDbContext>(...); // mặc định là Scoped
   builder.Services.AddScoped<IProductRepository, EfCoreProductRepository>();
   ```
3. `ProductsController` và `ProductService` **không cần sửa bất kỳ dòng code nào**! Đó chính là sức mạnh của DI + Interface.

*(Đọc thêm Slide 16: các dịch vụ có sẵn của .NET như `ILogger<T>`, `IOptions<T>`, `IHttpClientFactory` cũng hoạt động theo đúng cơ chế DI này.)*

---

## V. Lỗi thường gặp, Checklist 4 chốt & Kịch bản Demo 5 phút

### 1. 4 lỗi DI phổ biến & Cách sửa (Slide 18)

| Lỗi / Triệu chứng | Nguyên nhân gốc | Cách sửa đúng |
| :--- | :--- | :--- |
| **`InvalidOperationException: Unable to resolve service for type '...'`** | Quên đăng ký `interface -> class` trong `Program.cs`, hoặc đăng ký sai interface | Thêm `builder.Services.AddScoped<I..., ...>()` vào `Program.cs` |
| **Scoped service bị Singleton giữ (Captive Dependency)** | Inject một service `Scoped` (như `DbContext`) vào constructor của một `Singleton` | Đổi class cha sang `Scoped`, hoặc tạo scope tạm thời qua `IServiceScopeFactory` |
| **Dữ liệu bị lỗi / Race condition ở Singleton** | Singleton chứa biến thay đổi (mutable state) mà không đồng bộ | Bỏ state theo user ra khỏi Singleton, hoặc dùng `Lock` / cấu trúc thread-safe |
| **Gọi `provider.GetService<T>()` khắp nơi (Service Locator)** | Giấu dependency bên trong thân hàm thay vì khai báo qua constructor | Đưa dependency lên constructor để nhìn rõ mọi phụ thuộc của class |

### 2. Checklist 4 chốt trước khi nói "Done" (Slide 20)

1. **Registration:** Mọi interface được inject đều đã đăng ký implementation tương ứng trong `Program.cs`.
2. **Lifetime:** Không kéo `Scoped` (như `DbContext`) vào `Singleton`; `Singleton` có state phải thread-safe.
3. **Design:** Dùng Constructor Injection, tránh lạm dụng Service Locator (`GetService`).
4. **Verification:** Đã kiểm tra thực tế cả `/Products`, `/Lifetime` (có bấm Refresh) và chạy `dotnet test` pass 100%.

### 3. Kịch bản Live Demo 5 phút (Slide 21)

1. **Run (30 giây):** Chạy `dotnet test` (cho thấy toàn bộ test xanh) rồi `dotnet run --project DependencyInjectionMvcDemo`.
2. **Products (1.5 phút):** Mở `/Products`, chỉ ra dải luồng DI `ProductsController -> IProductService -> IProductRepository`, thử tìm kiếm hoặc thêm/sửa 1 sản phẩm và mở `Program.cs` cho thấy `ProductsController` không hề gọi `new`.
3. **Lifetime (1 phút):** Mở `/Lifetime`, chỉ rõ `T1 ≠ T2` nhưng `S1 = S2` và `G1 = G2` trong cùng 1 request.
4. **Refresh (1 phút):** Bấm Refresh (`F5`) trên `/Lifetime` để chứng minh `Scoped` đổi sang GUID mới còn `Singleton` giữ nguyên GUID cũ.
5. **Test (1 phút):** Mở `ProductServiceTests.cs` và `FakeProductRepository.cs` để chỉ cách DI giúp test `GetById(42)` mà không cần database hay HTTP server.

---

## VI. Tài liệu tham khảo chính thức (R1–R10)

Danh sách nguồn kỹ thuật ưu tiên từ **Microsoft Learn** (tương ứng Slide 22 & 23):

| Mã | Chủ đề | Liên kết chính thức | Lý do sử dụng |
| :--- | :--- | :--- | :--- |
| **[R1]** | Dependency injection in .NET | https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/ | Nền tảng khái niệm IoC, DI container và constructor injection |
| **[R2]** | Service lifetimes in .NET | https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/service-lifetimes | Phân biệt `Transient`, `Scoped`, `Singleton` và hành vi của `DbContext` |
| **[R3]** | Dependency injection into controllers in ASP.NET Core | https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/dependency-injection | Giải thích cách MVC framework tự động resolve dependency cho Controller |
| **[R4]** | Dependency injection guidelines | https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines | Quy tắc dispose service, tránh Captive Dependency, tránh Service Locator và đảm bảo thread-safety |
| **[R5]** | Get started with ASP.NET Core MVC | https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-mvc-app/start-mvc | Thiết lập project MVC từ template và luồng Controller–View |
| **[R6]** | .NET and .NET Core Support Policy | https://learn.microsoft.com/en-us/dotnet/core/releases-and-support | Kiểm tra phiên bản .NET LTS/STS (`net10.0`) khi khởi tạo project |
| **[R7]** | App startup in ASP.NET Core (`Program.cs`) | https://learn.microsoft.com/en-us/aspnet/core/fundamentals/startup | Hiểu vai trò Composition Root và cấu hình middleware pipeline |
| **[R8]** | Unit test controller logic in ASP.NET Core | https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/testing | Cô lập logic Controller/Service bằng Fake/Mock với xUnit |
| **[R9]** | Logging & Options pattern in .NET | https://learn.microsoft.com/en-us/dotnet/core/extensions/options | Mở rộng cách inject `ILogger<T>` và `IOptions<T>` |
| **[R10]** | Make HTTP requests using `IHttpClientFactory` | https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests | Mở rộng cách inject `IHttpClientFactory` để quản lý `HttpClient` an toàn |
