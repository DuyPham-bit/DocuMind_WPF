using Microsoft.EntityFrameworkCore;
using ProductManagerMvc.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký dịch vụ MVC (Controllers + Views)
//    Thông báo lỗi model binding (ô số để trống, nhập chữ vào ô số...) mặc định là tiếng Anh, đổi sang tiếng Việt.
builder.Services.AddControllersWithViews(options =>
{
    var messages = options.ModelBindingMessageProvider;
    messages.SetValueMustNotBeNullAccessor(_ => "Trường này không được để trống.");
    messages.SetMissingBindRequiredValueAccessor(field => $"Vui lòng nhập {field}.");
    messages.SetAttemptedValueIsInvalidAccessor((value, field) => $"Giá trị \"{value}\" không hợp lệ cho {field}.");
    messages.SetUnknownValueIsInvalidAccessor(field => $"Giá trị nhập cho {field} không hợp lệ.");
    messages.SetValueIsInvalidAccessor(value => $"Giá trị \"{value}\" không hợp lệ.");
    messages.SetValueMustBeANumberAccessor(field => $"{field} phải là một số.");
    messages.SetNonPropertyValueMustBeANumberAccessor(() => "Giá trị phải là một số.");
});

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

// Hiển thị giá theo định dạng Việt Nam (250.000 VNĐ). Ô nhập type="number" vẫn gửi số dạng invariant
// kèm field __Invariant nên model binding không bị lệch dấu thập phân.
var viCulture = new System.Globalization.CultureInfo("vi-VN");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(viCulture),
    SupportedCultures = [viCulture],
    SupportedUICultures = [viCulture]
});

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
