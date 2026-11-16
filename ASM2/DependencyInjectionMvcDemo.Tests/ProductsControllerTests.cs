using DependencyInjectionMvcDemo.Controllers;
using DependencyInjectionMvcDemo.Models;
using DependencyInjectionMvcDemo.Services;
using DependencyInjectionMvcDemo.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;

namespace DependencyInjectionMvcDemo.Tests;

public sealed class ProductsControllerTests
{
    private static ProductsController CreateController(params Product[] seed)
    {
        var repo = new FakeProductRepository(seed);
        var service = new ProductService(repo);
        return new ProductsController(service);
    }

    [Fact]
    public void Index_ReturnsViewWithAllProducts_WhenKeywordEmpty()
    {
        var controller = CreateController(
            new Product { Id = 1, Name = "Chuột gaming", Price = 250_000m, Stock = 12 },
            new Product { Id = 2, Name = "Bàn phím cơ", Price = 590_000m, Stock = 7 });

        var result = controller.Index(null);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IReadOnlyList<Product>>(viewResult.Model);
        Assert.Equal(2, model.Count);
    }

    [Fact]
    public void Details_ReturnsNotFound_WhenIdIsNullOrMissing()
    {
        var controller = CreateController();

        Assert.IsType<NotFoundResult>(controller.Details(null));
        Assert.IsType<NotFoundResult>(controller.Details(999));
    }

    [Fact]
    public void CreatePost_RedirectsToIndex_WhenModelValid()
    {
        var controller = CreateController();

        var result = controller.Create(new Product
        {
            Name = "Màn hình 24 inch",
            Price = 3_200_000m,
            Stock = 5
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ProductsController.Index), redirect.ActionName);
    }

    [Fact]
    public void CreatePost_ReturnsViewWithModel_WhenModelStateInvalid()
    {
        var controller = CreateController();
        controller.ModelState.AddModelError(nameof(Product.Name), "Required");
        var invalid = new Product { Name = "", Price = 0m, Stock = 0 };

        var result = controller.Create(invalid);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(invalid, viewResult.Model);
    }

    [Fact]
    public void EditPost_ReturnsNotFound_WhenRouteIdDiffersFromProductId()
    {
        var controller = CreateController(new Product { Id = 1, Name = "Chuột", Price = 100_000m, Stock = 2 });

        var result = controller.Edit(99, new Product { Id = 1, Name = "Chuột", Price = 100_000m, Stock = 2 });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void DeleteConfirmed_RedirectsToIndex_WhenProductExists()
    {
        var controller = CreateController(new Product { Id = 1, Name = "Chuột", Price = 100_000m, Stock = 2 });

        var result = controller.DeleteConfirmed(1);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ProductsController.Index), redirect.ActionName);
    }
}
