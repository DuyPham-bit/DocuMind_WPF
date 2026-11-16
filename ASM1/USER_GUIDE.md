# USER GUIDE: SETUP & CODE WEB MVC BẰNG C# VỚI AI AGENT (ASM1 - PRN212)

Tài liệu hướng dẫn từng bước dành cho người mới bắt đầu học **.NET / C#**, giúp bạn:
1. Hiểu và tự tay dựng ứng dụng Web **ASP.NET Core MVC** (`ProductManagerMvc`) kết nối **SQL Server** qua **Entity Framework Core**.
2. Biết cách chọn **AI Agent trong IDE**, viết **Prompt có cấu trúc** và kiểm soát AI an toàn khi làm một website cơ bản.

> **Ranh giới kiến thức (theo định hướng Assignment 1 - PRN212):**
> - **Nền tảng PRN212 được vận dụng:** C#, OOP (`Product`), LINQ (tìm kiếm dữ liệu), EF Core + SQL Server (CRUD), tư duy tách giao diện từ WPF.
> - **Công nghệ áp dụng theo yêu cầu Assignment:** ASP.NET Core MVC (Model - View - Controller + HTML/Razor View) và quy trình làm việc với AI Agent trong IDE.

---

## Mục lục
- [I. Làm Web MVC trong C# thế nào?](#i-làm-web-mvc-trong-c-thế-nào)
  - [1. Setup môi trường (Chọn 1 trong 2 IDE)](#1-setup-môi-trường-chọn-1-trong-2-ide)
  - [2. Chạy nhanh project mẫu trong thư mục `ASM1`](#2-chạy-nhanh-project-mẫu-trong-thư-mục-asm1)
  - [3. Tự tạo project MVC mới từ đầu bằng lệnh CLI](#3-tự-tạo-project-mvc-mới-từ-đầu-bằng-lệnh-cli)
  - [4. Hiểu kiến trúc MVC & luồng xử lý một HTTP Request](#4-hiểu-kiến-trúc-mvc--luồng-xử-lý-một-http-request)
  - [5. Giải thích chi tiết từng file code trong `ProductManagerMvc`](#5-giải-thích-chi-tiết-từng-file-code-trong-productmanagermvc)
- [II. Dùng AI trong IDE, AI Agent nào, và thế nào để làm Web cơ bản?](#ii-dùng-ai-trong-ide-ai-agent-nào-và-thế-nào-để-làm-web-cơ-bản)
  - [1. Phân biệt 3 mức AI hỗ trợ code trong IDE](#1-phân-biệt-3-mức-ai-hỗ-trợ-code-trong-ide)
  - [2. Nên dùng AI Agent nào? (GitHub Copilot Agent vs Cursor Agent)](#2-nên-dùng-ai-agent-nào-github-copilot-agent-vs-cursor-agent)
  - [3. Quy trình 5 bước thiết lập GitHub Copilot Agent](#3-quy-trình-5-bước-thiết-lập-github-copilot-agent)
  - [4. Công thức Prompt Engineering 5 phần (Context → Safety)](#4-công-thức-prompt-engineering-5-phần-context--safety)
  - [5. Workflow làm việc với AI Agent (Developer luôn là người quyết định)](#5-workflow-làm-việc-với-ai-agent-developer-luôn-là-người-quyết-định)
  - [6. Checklist 4 chốt QA & Bảo mật trước khi nói "Done"](#6-checklist-4-chốt-qa--bảo-mật-trước-khi-nói-done)
- [III. Kịch bản Demo 5 phút & Lỗi thường gặp](#iii-kịch-bản-demo-5-phút--lỗi-thường-gặp)
- [IV. Tài liệu tham khảo chính thức](#iv-tài-liệu-tham-khảo-chính-thức)

---

## I. Làm Web MVC trong C# thế nào?

### 1. Setup môi trường (Chọn 1 trong 2 IDE)

Bạn **không cần cài cả hai IDE** — hãy chọn 1 cách phù hợp nhất với máy của mình:

#### Yêu cầu chung trên máy Windows
1. **.NET SDK (.NET 10 / LTS):** Kiểm tra trong PowerShell/Terminal:
   ```powershell
   dotnet --version
   dotnet --info
   ```
2. **SQL Server:** Đảm bảo dịch vụ `SQL Server (MSSQLSERVER)` (hoặc SQL Server Express / LocalDB) đang ở trạng thái **Running**.
3. **EF Core CLI tool** (để chạy lệnh migration tạo bảng database):
   ```powershell
   dotnet tool install --global dotnet-ef
   ```

#### Lựa chọn A: Visual Studio (Khuyên dùng cho người mới học .NET)
1. Cài **Visual Studio** (Community miễn phí).
2. Trong Visual Studio Installer, tick chọn workload **ASP.NET and web development** (đã bao gồm .NET SDK và công cụ web).
3. Mở file `ASM1\ASM1.slnx`.
4. Kiểm tra chuỗi kết nối SQL Server trong `ProductManagerMvc\appsettings.json`.
5. Nhấn **`Ctrl + F5`** (Start Without Debugging) để chạy web và mở trình duyệt tự động.

#### Lựa chọn B: VS Code + Terminal (Nhẹ, phù hợp khi demo CLI)
1. Cài **.NET SDK**.
2. Cài **VS Code** và mở Extensions (`Ctrl + Shift + X`) để cài:
   - **C# Dev Kit** (tự kéo theo extension C# và IntelliCode)
   - **GitHub Copilot** (và **GitHub Copilot Chat**)
3. Mở thư mục `ASM1` (`File -> Open Folder...`).
4. Mở Terminal tích hợp (`Ctrl + ~`) và dùng lệnh `dotnet run` / `dotnet test`.

---

### 2. Chạy nhanh project mẫu trong thư mục `ASM1`

Cấu trúc giải pháp `ASM1`:
```text
ASM1/
├── ASM1.slnx                        # File Solution gom app web và project test
├── ProductManagerMvc/               # Ứng dụng Web ASP.NET Core MVC
│   ├── Controllers/
│   │   └── ProductsController.cs    # Nhận request, dùng LINQ + EF Core, trả View
│   ├── Data/
│   │   ├── AppDbContext.cs          # Cầu nối EF Core <-> SQL Server
│   │   └── DbInitializer.cs         # Tự chạy Migration & seed 3 sản phẩm mẫu
│   ├── Migrations/                  # Lịch sử thay đổi schema bảng Products
│   ├── Models/
│   │   └── Product.cs               # Class C# Product + DataAnnotations validation
│   ├── Views/
│   │   ├── Shared/_Layout.cshtml    # Khung giao diện chung (header, footer, CSS)
│   │   └── Products/                # Index, Details, Create, Edit, Delete, _ProductFormFields
│   ├── wwwroot/css/site.css         # CSS giao diện phẳng, sạch, responsive
│   ├── appsettings.json             # Chuỗi kết nối SQL Server (DefaultConnection)
│   └── Program.cs                   # Khởi tạo DI, DbContext, Routing mặc định
├── ProductManagerMvc.Tests/         # Unit tests (xUnit + EF Core InMemory)
│   ├── ProductValidationTests.cs    # Kiểm tra rule Validate Name, Price, Stock
│   └── ProductsControllerTests.cs   # Kiểm tra Search LINQ, CRUD, 404 NotFound
└── USER_GUIDE.md                    # Tài liệu hướng dẫn này
```

#### Các bước chạy trên máy bạn:
```powershell
# Bước 1: Di chuyển vào thư mục ASM1
cd d:\CN5.1\PRN212\26.FA.PRN212\Project-Prn212\ASM1

# Bước 2: Khôi phục NuGet packages & build kiểm tra lỗi biên dịch
dotnet restore
dotnet build

# Bước 3: Chạy toàn bộ Unit Tests
dotnet test

# Bước 4: Cập nhật database lên SQL Server (tùy chọn - Program.cs cũng tự Migrate khi chạy)
cd ProductManagerMvc
dotnet ef database update

# Bước 5: Chạy web app
dotnet run
```
Sau khi chạy `dotnet run`, terminal sẽ in ra địa chỉ localhost (ví dụ `http://localhost:5xxx` hoặc `https://localhost:7xxx`). Mở trình duyệt và truy cập địa chỉ đó — ứng dụng sẽ mở thẳng danh sách sản phẩm với 3 sản phẩm mẫu (`Chuột gaming`, `Bàn phím cơ`, `Tai nghe USB`).

> **Mẹo sửa lỗi kết nối SQL Server:** Mở `ProductManagerMvc\appsettings.json`:
> - Nếu dùng SQL Server mặc định (`MSSQLSERVER`): `"Server=.;Database=ProductManagerDB;Trusted_Connection=True;TrustServerCertificate=True;"`
> - Nếu dùng SQL Express: đổi `Server=.` thành `Server=.\SQLEXPRESS`.
> - Nếu dùng LocalDB: đổi `Server=.` thành `Server=(localdb)\mssqllocaldb`.

---

### 3. Tự tạo project MVC mới từ đầu bằng lệnh CLI

Nếu bạn muốn tự tay tạo lại project từ số 0 để thực hành:

```powershell
# 1. Tạo project MVC mới
dotnet new mvc -n ProductManagerMvc
cd ProductManagerMvc

# 2. Cài gói EF Core cho SQL Server (chọn đúng major version với .NET SDK, ví dụ 10.0.14 cho .NET 10)
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 10.0.14
dotnet add package Microsoft.EntityFrameworkCore.Design --version 10.0.14

# 3. Sau khi viết Models/Product.cs, Data/AppDbContext.cs và cấu hình Program.cs:
dotnet ef migrations add InitialCreate
dotnet ef database update

# 4. Chạy thử
dotnet run
```

---

### 4. Hiểu kiến trúc MVC & luồng xử lý một HTTP Request

**MVC** chia ứng dụng web thành 3 phần tách biệt trách nhiệm (tương tự tư duy tách UI / Logic / Data bạn đã học ở WPF trong PRN212):
- **M — Model (`Models/Product.cs`):** Mô tả cấu trúc dữ liệu (class C# theo OOP) và quy tắc kiểm tra dữ liệu (`[Required]`, `[Range]`).
- **C — Controller (`Controllers/ProductsController.cs`):** Tiếp nhận request từ trình duyệt, kiểm tra dữ liệu, gọi `AppDbContext` + **LINQ** để đọc/ghi SQL Server, rồi quyết định trả về trang nào.
- **V — View (`Views/Products/*.cshtml`):** File Razor kết hợp HTML + cú pháp `@Model` để hiển thị danh sách bảng hoặc form nhập liệu cho người dùng.

**Ví dụ luồng xử lý khi người dùng tìm kiếm `/Products?q=gaming`:**
1. **Browser:** Gửi HTTP `GET /Products?q=gaming`.
2. **Routing (`Program.cs`):** Ánh xạ URL vào `ProductsController`, action `Index(string? q)`.
3. **Controller:** Nhận `q = "gaming"`, dùng LINQ `.Where(p => p.Name.Contains(keyword) || ...)` trên `_db.Products.AsNoTracking()`.
4. **EF Core → SQL Server:** Dịch câu lệnh LINQ thành câu SQL `SELECT ... FROM Products WHERE ...` và lấy dữ liệu về thành `List<Product>`.
5. **HTML View (`Index.cshtml`):** Nhận `List<Product>`, render thành bảng HTML hoàn chỉnh và trả lại trình duyệt.

---

### 5. Giải thích chi tiết từng file code trong `ProductManagerMvc`

#### (a) Model: `Models/Product.cs`
Class `Product` đại diện cho 1 dòng trong bảng `Products`:
- `Id`: Khóa chính tự tăng (EF Core tự nhận biết thuộc tính tên `Id` là Primary Key).
- `[Required]`: Không cho phép để trống `Name`.
- `[Range(1, 999999999)]`: Giá phải lớn hơn 0 (ngăn nhập `Price = 0` hoặc số âm).
- `[Range(0, 100000)]`: Tồn kho không được âm.
- `[Column(TypeName = "decimal(18,2)")]`: Khai báo kiểu cột tiền tệ chính xác trong SQL Server.

#### (b) DbContext & Seed Data: `Data/AppDbContext.cs` và `Data/DbInitializer.cs`
- `AppDbContext` kế thừa `DbContext`, chứa `public DbSet<Product> Products => Set<Product>();`.
- `DbInitializer.InitializeAsync(db)` gọi `await db.Database.MigrateAsync()` để đảm bảo bảng `Products` đã được tạo từ Migration, sau đó kiểm tra nếu bảng rỗng (`!await db.Products.AnyAsync()`) thì thêm sẵn 3 sản phẩm demo.

#### (c) Controller: `Controllers/ProductsController.cs`
Áp dụng nguyên tắc **GET để đọc, POST để thay đổi dữ liệu**:
- `Index(string? q)`: Đọc danh sách, dùng `.AsNoTracking()` giúp query nhẹ hơn vì chỉ đọc không sửa, lọc LINQ nếu người dùng có nhập từ khóa `q`.
- `Details(int? id)`: Nếu `id == null` hoặc không tìm thấy trong DB -> trả `NotFound()` (HTTP 404).
- `Create()` (GET) & `Create([Bind(...)] Product product)` (POST):
  - Có `[ValidateAntiForgeryToken]` chống tấn công giả mạo request (CSRF).
  - Dùng `[Bind("Name,Description,Price,Stock")]` để tránh lỗi bảo mật over-posting.
  - Kiểm tra `if (!ModelState.IsValid) return View(product);` trước khi lưu.
- `Edit(int id, ...)` (POST): Kiểm tra `id != product.Id`, tìm bản ghi hiện tại bằng `FindAsync(id)`, nếu null trả `NotFound()`, cập nhật các trường hợp lệ và `SaveChangesAsync()`.
- `Delete(int? id)` (GET) & `DeleteConfirmed(int id)` (POST): Hiện trang xác nhận trước khi xóa vĩnh viễn.

#### (d) Views: `Views/Products/`
- `_ProductFormFields.cshtml`: Partial View dùng chung cho cả `Create.cshtml` và `Edit.cshtml` để không lặp lại code HTML form.
- Sử dụng **Tag Helpers** (`asp-action`, `asp-for`, `asp-validation-for`): ASP.NET Core tự động sinh `<input>`, tự điền giá trị, tự chèn Anti-Forgery hidden token vào `<form method="post">` và tự hiển thị thông báo lỗi validation ngay bên dưới ô nhập.

---

## II. Dùng AI trong IDE, AI Agent nào, và thế nào để làm Web cơ bản?

### 1. Phân biệt 3 mức AI hỗ trợ code trong IDE

| Mức độ | Cách hoạt động | Dùng khi nào | Rủi ro cần lưu ý |
| :--- | :--- | :--- | :--- |
| **1. Completion (Inline)** | Gợi ý vài dòng code ngay tại vị trí con trỏ khi bạn đang gõ | Viết nhanh getter/setter, vòng lặp, HTML lặp lại | Đọc kỹ dòng gợi ý, tránh Tab nhầm biến sai tên |
| **2. Ask / Chat** | Hỏi-đáp trong khung chat; AI giải thích code, lỗi biên dịch, hoặc đề xuất kiến trúc | Học khái niệm mới, hỏi nguyên nhân lỗi, lên plan | Copy code vào phải tự kiểm tra lại context file |
| **3. Agent Mode** | AI **tự đọc nhiều file trong workspace, tự lên kế hoạch sửa đa file, và đề xuất chạy lệnh terminal** | Thêm tính năng mới (ví dụ thêm Search, viết Unit Test, sửa bug nhiều file) | **Quyền hạn lớn nhất** → bắt buộc giới hạn scope, review diff và phê duyệt từng command |

---

### 2. Nên dùng AI Agent nào? (GitHub Copilot Agent vs Cursor Agent)

Cả hai công cụ đều có thể xây dựng ứng dụng ASP.NET Core MVC tốt; bạn chọn theo IDE mình đang dùng:

- **Khuyến nghị chính cho sinh viên / người mới .NET:** **GitHub Copilot Agent**
  - Tích hợp sẵn trong **Visual Studio** và **VS Code** (miễn phí qua gói GitHub Education / Copilot Free tier).
  - Có đầy đủ 3 chế độ **Completion / Ask / Agent**, sửa nhiều file cùng lúc và hỏi xác nhận trước khi chạy lệnh terminal.
  - Phù hợp nhất khi học bài bản với IDE truyền thống của hệ sinh thái Microsoft/.NET.
- **Lựa chọn thay thế (AI-first IDE):** **Cursor Agent**
  - Giao diện xây dựng trên nền VS Code, mạnh về tìm kiếm ngữ cảnh toàn bộ codebase, xem diff trực quan và có **Checkpoint Rollback** để hoàn tác nhanh nếu AI sửa sai.
  - Phù hợp khi bạn thích quy trình làm việc ưu tiên AI và quen làm .NET qua terminal (`dotnet` CLI).

---

### 3. Quy trình 5 bước thiết lập GitHub Copilot Agent

1. **Cài đặt:** Cài extension **GitHub Copilot** + **GitHub Copilot Chat** trong VS Code (hoặc bật GitHub Copilot tích hợp sẵn trong Visual Studio).
2. **Đăng nhập:** Kết nối tài khoản GitHub có quyền sử dụng Copilot.
3. **Chọn chế độ Agent:** Mở khung Copilot Chat → chuyển dropdown chế độ từ **Ask** sang **Agent**.
4. **Giao task rõ ràng:** Đính kèm file liên quan (ví dụ `#Product.cs`, `#ProductsController.cs`) và gửi prompt có cấu trúc.
5. **Review & Phê duyệt:** Xem kỹ **Diff** ở từng file được sửa; **KHÔNG bật auto-approve command** cho các lệnh đụng vào database hay xóa file — chỉ nhấn *Allow/Run* khi đọc hiểu rõ lệnh đó làm gì.

---

### 4. Công thức Prompt Engineering 5 phần (`Context → Task → Scope → Acceptance → Safety`)

AI Agent không thể đoán đúng ý bạn nếu prompt quá chung chung. Hãy so sánh:

- **Prompt mơ hồ (NÊN TRÁNH):**
  > *"Làm giúp tôi web quản lý sản phẩm."*
  *(Thiếu stack công nghệ, thiếu phạm vi file, không có tiêu chí kiểm thử và ràng buộc an toàn — Agent rất dễ tạo sai framework hoặc sửa lung tung.)*

- **Prompt có cấu trúc 5 phần (NÊN DÙNG):**
  ```text
  Context: Dự án ASP.NET Core MVC (.NET 10), dùng C#, LINQ, EF Core + SQL Server.
           Model Product gồm Id, Name, Description, Price, Stock.
  Task: Thêm tính năng tìm kiếm sản phẩm theo từ khóa q (khớp Name hoặc Description).
  Scope: Chỉ chỉnh sửa Controllers/ProductsController.cs và Views/Products/Index.cshtml.
  Acceptance:
  - GET /Products?q=chuột trả về đúng danh sách khớp bằng LINQ AsNoTracking().
  - Có nút "Xóa lọc" khi đang tìm kiếm.
  - Chạy dotnet build và dotnet test pass 100%.
  Safety: Không xóa database hoặc sửa migration cũ; hỏi trước khi chạy lệnh terminal.
  ```

#### Bộ 3 Prompt mẫu để bạn tự thực hành xây web cơ bản từ đầu:
1. **Prompt 1 — Tạo Model & DbContext:**
   - *Context:* Project MVC mới tạo bằng `dotnet new mvc`, đã cài `Microsoft.EntityFrameworkCore.SqlServer`.
   - *Task:* Tạo `Models/Product.cs` (với DataAnnotations validation) và `Data/AppDbContext.cs`, cấu hình `Program.cs` đọc `ConnectionStrings:DefaultConnection`.
   - *Scope:* `Models/Product.cs`, `Data/AppDbContext.cs`, `appsettings.json`, `Program.cs`.
   - *Acceptance:* `dotnet build` không có lỗi biên dịch.
   - *Safety:* Không tự ý chạy `dotnet ef database drop`.
2. **Prompt 2 — Tạo CRUD Controller & Views:**
   - *Task:* Tạo `ProductsController` đủ 5 chức năng List, Details, Create, Edit, Delete và các Razor View tương ứng.
   - *Acceptance:* POST action đều có `[ValidateAntiForgeryToken]` và kiểm tra `ModelState.IsValid`; trả `NotFound()` (404) khi `id` không tồn tại.
3. **Prompt 3 — Viết Unit Test kiểm chứng:**
   - *Task:* Viết xUnit test cho validation của `Product` và logic Search/CRUD/404 của `ProductsController` dùng `EF Core InMemory`.
   - *Acceptance:* `dotnet test` chạy pass tất cả test cases.

---

### 5. Workflow làm việc với AI Agent (Developer luôn là người quyết định)

Đừng bấm "Accept All" một cách mù quáng. Hãy đi theo vòng lặp 5 bước:

```text
[1. Đọc yêu cầu đề bài]
        ↓
[2. Lập Plan ngắn (file nào sẽ tạo/sửa)]
        ↓
[3. Giao task cho Agent Edit theo phạm vi nhỏ]
        ↓
[4. Review Diff từng file (kiểm tra logic, LINQ, bảo mật)]
        ↓
[5. Chạy dotnet build & dotnet test + test trên trình duyệt]
        │
        ├── Nếu FAIL: Copy nguyên thông báo lỗi thật dán cho Agent → sửa đúng scope → quay lại bước 4
        └── Nếu PASS toàn bộ: Hoàn thành task nhỏ, chuyển sang task tiếp theo
```

> **Nguyên tắc vàng:** *"Agent tự báo Done chưa phải bằng chứng hoàn thành. Kết quả chạy `dotnet build`, `dotnet test` và thao tác thực tế trên trình duyệt mới là bằng chứng."*

---

### 6. Checklist 4 chốt QA & Bảo mật trước khi nói "Done"

1. **Validation:**
   - Model có đủ `[Required]`, `[Range]`?
   - Controller POST có kiểm tra `if (!ModelState.IsValid) return View(product);`?
2. **Bảo mật Form & Secrets:**
   - Mọi form/action `POST` (`Create`, `Edit`, `Delete`) đều có `[ValidateAntiForgeryToken]`?
   - Có dùng `[Bind(...)]` hoặc cập nhật từng thuộc tính để chống over-posting?
   - Không lưu mật khẩu/API key thật lên Git?
3. **Database & Migration:**
   - Đã đọc kỹ file `Migrations/*_InitialCreate.cs` trước khi chạy `database update`?
   - Không có lệnh phá hủy dữ liệu (`EnsureDeleted`, `database drop`) trong code khởi động?
4. **Verification thực tế:**
   - `dotnet build` exit code 0?
   - `dotnet test` pass 100%?
   - Đã test đủ trên trình duyệt: xem danh sách, thêm mới, tìm kiếm, sửa, xóa, nhập dữ liệu sai (Name trống / Price = 0), và thử URL sai `/Products/Details/999` ra 404?

---

## III. Kịch bản Demo 5 phút & Lỗi thường gặp

### Kịch bản Demo 5 phút (theo Slide 21)
1. **Run (30s):** Mở terminal chạy `dotnet test` (cho thấy 13/13 tests pass) rồi `dotnet run`, mở trình duyệt tại `/Products`.
2. **CRUD + Search (2 phút):**
   - Thấy sẵn 3 sản phẩm mẫu (`Chuột gaming`, `Bàn phím cơ`, `Tai nghe USB`).
   - Bấm **Thêm sản phẩm** → nhập 1 sản phẩm mới → lưu thành công.
   - Nhập từ khóa vào ô **Tìm kiếm** → bấm Tìm kiếm → bấm **Sửa** để đổi giá/tồn kho.
3. **Validation & 404 (1 phút):**
   - Vào **Thêm sản phẩm**, để trống `Name` hoặc nhập `Price = 0` → thấy thông báo lỗi đỏ ngay dưới ô nhập, không lưu rác vào DB.
   - Thử truy cập `/Products/Details/999` → nhận phản hồi 404 chuẩn.
4. **Minh họa dùng AI Agent (1 phút):**
   - Mở Copilot Agent, chỉ rõ Prompt có cấu trúc (ví dụ kiểm tra hoặc tinh chỉnh Search trong `ProductsController` + `Index.cshtml`).
5. **Review Diff & Build (30s):**
   - Mở Git/IDE diff xem đúng phạm vi file vừa sửa, chạy `dotnet build` xác nhận không lỗi.

### Lỗi thường gặp cho người mới .NET
- **Lỗi `NU1202: Package ... 11.0.0 is not compatible with net10.0`:** Khi chạy `dotnet add package` không ghi `--version`, NuGet có thể tải bản mới hơn SDK hiện tại. Cách sửa: thêm `--version 10.0.14` (khớp với `net10.0`).
- **Lỗi kết nối SQL Server (`Login failed` / `network-related error`):** Kiểm tra Windows Service `SQL Server (MSSQLSERVER)` đã `Running` chưa, và chỉnh lại `Server=` trong `appsettings.json` (`Server=.` hoặc `Server=.\SQLEXPRESS` hoặc `Server=(localdb)\mssqllocaldb`).
- **Lỗi `dotnet ef` không tìm thấy:** Chạy `dotnet tool install --global dotnet-ef` rồi mở lại terminal.

---

## IV. Tài liệu tham khảo chính thức

- **Microsoft .NET Support Policy (chọn phiên bản LTS):** https://dotnet.microsoft.com/en-us/platform/support/policy
- **Microsoft Learn — ASP.NET Core MVC Tutorial:** https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-mvc-app/start-mvc
- **EF Core — SQL Server Provider & Migrations:** https://learn.microsoft.com/en-us/ef/core/providers/sql-server/
- **Microsoft Learn — Model Validation & Anti-CSRF:** https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation
- **GitHub Copilot Agent Mode Docs:** https://docs.github.com/en/copilot/how-tos/copilot-in-your-ide/use-copilot-agents/use-agent-mode
- **Cursor Agent Docs:** https://cursor.com/help/ai-features/agent
