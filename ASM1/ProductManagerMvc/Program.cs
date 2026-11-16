using Microsoft.EntityFrameworkCore;
using ProductManagerMvc.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký dịch vụ MVC (Controllers + Views)
builder.Services.AddControllersWithViews();

// 2. Đăng ký AppDbContext dùng SQL Server (lấy chuỗi kết nối từ appsettings.json)
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:DefaultConnection trong appsettings.json.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

// 3. Tự động áp dụng Migration & seed 3 sản phẩm mẫu nếu database chưa có dữ liệu
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbInitializer.InitializeAsync(db);
}

// 4. Cấu hình HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

// 5. Định tuyến mặc định mở thẳng trang danh sách sản phẩm (/Products/Index)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Products}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
