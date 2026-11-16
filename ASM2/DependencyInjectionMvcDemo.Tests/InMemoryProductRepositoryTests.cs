using DependencyInjectionMvcDemo.Models;
using DependencyInjectionMvcDemo.Repositories;

namespace DependencyInjectionMvcDemo.Tests;

public sealed class InMemoryProductRepositoryTests
{
    [Fact]
    public void Constructor_SeedsThreeDefaultProductsFromSlide14()
    {
        var repo = new InMemoryProductRepository();

        var items = repo.GetAll();

        Assert.Equal(3, items.Count);
        Assert.Collection(
            items,
            first =>
            {
                Assert.Equal(1, first.Id);
                Assert.Equal("Chuột gaming", first.Name);
                Assert.Equal(250_000m, first.Price);
                Assert.Equal(12, first.Stock);
            },
            second =>
            {
                Assert.Equal(2, second.Id);
                Assert.Equal("Bàn phím cơ", second.Name);
                Assert.Equal(590_000m, second.Price);
                Assert.Equal(7, second.Stock);
            },
            third =>
            {
                Assert.Equal(3, third.Id);
                Assert.Equal("Tai nghe USB", third.Name);
                Assert.Equal(320_000m, third.Price);
                Assert.Equal(15, third.Stock);
            });
    }

    [Fact]
    public void AddUpdateDelete_WorkCorrectlyAndReturnDefensiveCopies()
    {
        var repo = new InMemoryProductRepository();

        var added = repo.Add(new Product { Name = "Lót chuột", Price = 120_000m, Stock = 20 });
        Assert.Equal(4, added.Id);

        // Mutating returned object must not silently mutate repository state
        added.Name = "Mutated outside";
        var fetched = repo.GetById(4);
        Assert.NotNull(fetched);
        Assert.Equal("Lót chuột", fetched.Name);

        Assert.True(repo.Update(new Product { Id = 4, Name = "Lót chuột XXL", Price = 180_000m, Stock = 10 }));
        Assert.Equal("Lót chuột XXL", repo.GetById(4)!.Name);

        Assert.True(repo.Delete(4));
        Assert.Null(repo.GetById(4));
    }

    [Fact]
    public void Add_IsThreadSafe_WhenCalledConcurrentlyAsSingleton()
    {
        var repo = new InMemoryProductRepository();

        Parallel.For(0, 50, i =>
        {
            repo.Add(new Product { Name = $"Product {i}", Price = 10_000m + i, Stock = i });
        });

        var all = repo.GetAll();
        Assert.Equal(53, all.Count);
        Assert.Equal(53, all.Select(p => p.Id).Distinct().Count());
    }
}
