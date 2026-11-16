using DependencyInjectionMvcDemo.Models;
using DependencyInjectionMvcDemo.Services;
using DependencyInjectionMvcDemo.Tests.Fakes;

namespace DependencyInjectionMvcDemo.Tests;

public sealed class ProductServiceTests
{
    [Fact]
    public void GetById_ReturnsMatchingProduct_FromInjectedFakeRepository()
    {
        // Arrange: chuẩn bị FakeRepository chứa sản phẩm Id = 42 (Slide 15)
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
        Assert.Equal(750_000m, result.Price);
    }

    [Fact]
    public void GetById_ReturnsNull_WhenProductDoesNotExist()
    {
        var fakeRepo = new FakeProductRepository();
        var service = new ProductService(fakeRepo);

        var result = service.GetById(999);

        Assert.Null(result);
    }

    [Fact]
    public void GetAll_FiltersByKeyword_CaseInsensitive()
    {
        var fakeRepo = new FakeProductRepository([
            new Product { Id = 1, Name = "Chuột gaming", Description = "DPI cao", Price = 250_000m, Stock = 12 },
            new Product { Id = 2, Name = "Bàn phím cơ", Description = "Switch xanh", Price = 590_000m, Stock = 7 }
        ]);
        var service = new ProductService(fakeRepo);

        var result = service.GetAll("GAMING");

        Assert.Single(result);
        Assert.Equal("Chuột gaming", result[0].Name);
    }

    [Fact]
    public void Create_TrimsNameAndDescription_AndAssignsId()
    {
        var fakeRepo = new FakeProductRepository();
        var service = new ProductService(fakeRepo);

        var created = service.Create(new Product
        {
            Name = "  Webcam HD  ",
            Description = "  1080p USB  ",
            Price = 450_000m,
            Stock = 4
        });

        Assert.True(created.Id > 0);
        Assert.Equal("Webcam HD", created.Name);
        Assert.Equal("1080p USB", created.Description);
    }

    [Fact]
    public void Create_ThrowsArgumentException_WhenNameIsBlank()
    {
        var fakeRepo = new FakeProductRepository();
        var service = new ProductService(fakeRepo);

        Assert.Throws<ArgumentException>(() => service.Create(new Product
        {
            Name = "   ",
            Price = 100_000m,
            Stock = 1
        }));
    }

    [Fact]
    public void UpdateAndDelete_ReturnFalse_WhenTargetMissing()
    {
        var fakeRepo = new FakeProductRepository();
        var service = new ProductService(fakeRepo);

        Assert.False(service.Update(new Product { Id = 404, Name = "Missing", Price = 10_000m, Stock = 1 }));
        Assert.False(service.Delete(404));
    }
}
