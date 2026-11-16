using DependencyInjectionMvcDemo.Interfaces;
using DependencyInjectionMvcDemo.Repositories;
using DependencyInjectionMvcDemo.Services;

var builder = WebApplication.CreateBuilder(args);

// =========================================================================
// COMPOSITION ROOT (Slide 8 & Slide 11)
// Chỉ Program.cs biết concrete class nào được dùng; Controller và Service
// chỉ phụ thuộc vào interface (abstraction).
// =========================================================================
builder.Services.AddControllersWithViews();

// 1. Tầng dữ liệu + nghiệp vụ cho /Products
builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();

// 2. Ba marker minh họa Service Lifetime cho /Lifetime
builder.Services.AddTransient<ITransientMarker, TransientMarker>();
builder.Services.AddScoped<IScopedMarker, ScopedMarker>();
builder.Services.AddSingleton<ISingletonMarker, SingletonMarker>();
builder.Services.AddScoped<ILifetimeSnapshotService, LifetimeSnapshotService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

// Mở thẳng trang /Products khi truy cập "/" (Slide 14 & Slide 21)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Products}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
