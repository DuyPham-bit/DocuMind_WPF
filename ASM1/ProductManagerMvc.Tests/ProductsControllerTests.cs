using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductManagerMvc.Controllers;
using ProductManagerMvc.Data;
using ProductManagerMvc.Models;

namespace ProductManagerMvc.Tests;

public class ProductsControllerTests
{
    private static AppDbContext CreateInMemoryDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var db = new AppDbContext(options);
        db.Products.AddRange(
            new Product { Id = 1, Name = "Chuột gaming", Description = "Chuột RGB 16000 DPI", Price = 250000, Stock = 12 },
            new Product { Id = 2, Name = "Bàn phím cơ", Description = "Switch Blue có dây", Price = 590000, Stock = 7 },
            new Product { Id = 3, Name = "Tai nghe USB", Description = "Âm thanh vòm 7.1", Price = 320000, Stock = 15 }
        );
        db.SaveChanges();
        db.ChangeTracker.Clear();
        return db;
    }

    [Fact]
    public async Task Index_WithoutQuery_ReturnsAllProducts()
    {
        await using var db = CreateInMemoryDb(nameof(Index_WithoutQuery_ReturnsAllProducts));
        var controller = new ProductsController(db);

        var result = await controller.Index(null);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<Product>>(viewResult.Model);
        Assert.Equal(3, model.Count);
    }

    [Fact]
    public async Task Index_WithSearchQuery_FiltersByNameOrDescription()
    {
        await using var db = CreateInMemoryDb(nameof(Index_WithSearchQuery_FiltersByNameOrDescription));
        var controller = new ProductsController(db);

        var result = await controller.Index("gaming");

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<Product>>(viewResult.Model);
        var item = Assert.Single(model);
        Assert.Equal("Chuột gaming", item.Name);
    }

    [Fact]
    public async Task Details_WhenIdMissingOrNotFound_ReturnsNotFound()
    {
        await using var db = CreateInMemoryDb(nameof(Details_WhenIdMissingOrNotFound_ReturnsNotFound));
        var controller = new ProductsController(db);

        Assert.IsType<NotFoundResult>(await controller.Details(null));
        Assert.IsType<NotFoundResult>(await controller.Details(999));
    }

    [Fact]
    public async Task Create_ValidProduct_PersistsAndRedirectsToIndex()
    {
        await using var db = CreateInMemoryDb(nameof(Create_ValidProduct_PersistsAndRedirectsToIndex));
        var controller = new ProductsController(db);
        var newProduct = new Product
        {
            Name = "Màn hình 24 inch",
            Description = "Full HD IPS 100Hz",
            Price = 2490000,
            Stock = 4
        };

        var result = await controller.Create(newProduct);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ProductsController.Index), redirect.ActionName);
        Assert.Equal(4, await db.Products.CountAsync());
    }

    [Fact]
    public async Task Create_InvalidModelState_ReturnsViewWithoutSaving()
    {
        await using var db = CreateInMemoryDb(nameof(Create_InvalidModelState_ReturnsViewWithoutSaving));
        var controller = new ProductsController(db);
        controller.ModelState.AddModelError(nameof(Product.Name), "Required");
        var invalid = new Product { Name = "", Price = 0, Stock = 1 };

        var result = await controller.Create(invalid);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(invalid, viewResult.Model);
        Assert.Equal(3, await db.Products.CountAsync());
    }

    [Fact]
    public async Task Edit_ValidProduct_UpdatesAndRedirectsToIndex()
    {
        await using var db = CreateInMemoryDb(nameof(Edit_ValidProduct_UpdatesAndRedirectsToIndex));
        var controller = new ProductsController(db);
        var updated = new Product
        {
            Id = 1,
            Name = "Chuột gaming Pro",
            Description = "Phiên bản nâng cấp",
            Price = 350000,
            Stock = 20
        };

        var result = await controller.Edit(1, updated);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ProductsController.Index), redirect.ActionName);
        var entity = await db.Products.FindAsync(1);
        Assert.NotNull(entity);
        Assert.Equal("Chuột gaming Pro", entity!.Name);
        Assert.Equal(350000, entity.Price);
    }

    [Fact]
    public async Task DeleteConfirmed_ExistingId_RemovesProductAndRedirects()
    {
        await using var db = CreateInMemoryDb(nameof(DeleteConfirmed_ExistingId_RemovesProductAndRedirects));
        var controller = new ProductsController(db);

        var result = await controller.DeleteConfirmed(2);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ProductsController.Index), redirect.ActionName);
        Assert.Null(await db.Products.FindAsync(2));
        Assert.Equal(2, await db.Products.CountAsync());
    }

    [Fact]
    public async Task Edit_IdMismatchOrMissingProduct_ReturnsNotFound()
    {
        await using var db = CreateInMemoryDb(nameof(Edit_IdMismatchOrMissingProduct_ReturnsNotFound));
        var controller = new ProductsController(db);

        var mismatch = new Product { Id = 2, Name = "Sai id", Price = 1000, Stock = 1 };
        Assert.IsType<NotFoundResult>(await controller.Edit(1, mismatch));

        var missing = new Product { Id = 999, Name = "Không tồn tại", Price = 1000, Stock = 1 };
        Assert.IsType<NotFoundResult>(await controller.Edit(999, missing));

        var untouched = await db.Products.FindAsync(1);
        Assert.Equal("Chuột gaming", untouched!.Name);
    }

    [Fact]
    public async Task DeleteConfirmed_MissingId_ReturnsNotFoundAndKeepsData()
    {
        await using var db = CreateInMemoryDb(nameof(DeleteConfirmed_MissingId_ReturnsNotFoundAndKeepsData));
        var controller = new ProductsController(db);

        Assert.IsType<NotFoundResult>(await controller.DeleteConfirmed(999));
        Assert.Equal(3, await db.Products.CountAsync());
    }
}
