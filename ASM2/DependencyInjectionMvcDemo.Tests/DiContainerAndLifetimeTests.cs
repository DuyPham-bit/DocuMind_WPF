using DependencyInjectionMvcDemo.Interfaces;
using DependencyInjectionMvcDemo.Repositories;
using DependencyInjectionMvcDemo.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjectionMvcDemo.Tests;

public sealed class DiContainerAndLifetimeTests
{
    private static ServiceProvider BuildValidatedContainer()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IProductRepository, InMemoryProductRepository>();
        services.AddScoped<IProductService, ProductService>();
        services.AddTransient<ITransientMarker, TransientMarker>();
        services.AddScoped<IScopedMarker, ScopedMarker>();
        services.AddSingleton<ISingletonMarker, SingletonMarker>();
        services.AddScoped<ILifetimeSnapshotService, LifetimeSnapshotService>();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
    }

    [Fact]
    public void Container_ValidatesScopesAndBuildsWithoutCaptiveDependencies()
    {
        using var provider = BuildValidatedContainer();
        using var scope = provider.CreateScope();

        var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
        Assert.Equal(3, productService.GetAll().Count);
    }

    [Fact]
    public void Transient_CreatesDifferentGuid_AtTwoInjectionPointsInSameRequest()
    {
        using var provider = BuildValidatedContainer();
        using var scope = provider.CreateScope();

        var directTransient = scope.ServiceProvider.GetRequiredService<ITransientMarker>();
        var snapshot = scope.ServiceProvider.GetRequiredService<ILifetimeSnapshotService>();

        // Slide 10: T1 != T2 trong cùng một request
        Assert.NotEqual(directTransient.Id, snapshot.TransientId);
    }

    [Fact]
    public void Scoped_SharesSameGuidWithinOneScope_AndChangesAcrossScopes()
    {
        using var provider = BuildValidatedContainer();

        Guid request1Point1;
        Guid request1Point2;
        using (var scope1 = provider.CreateScope())
        {
            request1Point1 = scope1.ServiceProvider.GetRequiredService<IScopedMarker>().Id;
            request1Point2 = scope1.ServiceProvider.GetRequiredService<ILifetimeSnapshotService>().ScopedId;
        }

        Guid request2Point1;
        using (var scope2 = provider.CreateScope())
        {
            request2Point1 = scope2.ServiceProvider.GetRequiredService<IScopedMarker>().Id;
        }

        // Slide 10: S1 = S2 trong 1 request, đổi sau khi refresh (scope mới)
        Assert.Equal(request1Point1, request1Point2);
        Assert.NotEqual(request1Point1, request2Point1);
    }

    [Fact]
    public void Singleton_SharesSameGuidWithinOneScope_AndAcrossScopes()
    {
        using var provider = BuildValidatedContainer();

        Guid request1Point1;
        Guid request1Point2;
        using (var scope1 = provider.CreateScope())
        {
            request1Point1 = scope1.ServiceProvider.GetRequiredService<ISingletonMarker>().Id;
            request1Point2 = scope1.ServiceProvider.GetRequiredService<ILifetimeSnapshotService>().SingletonId;
        }

        Guid request2Point1;
        using (var scope2 = provider.CreateScope())
        {
            request2Point1 = scope2.ServiceProvider.GetRequiredService<ISingletonMarker>().Id;
        }

        // Slide 10: G1 = G2 trong 1 request và giữ nguyên sau khi refresh
        Assert.Equal(request1Point1, request1Point2);
        Assert.Equal(request1Point1, request2Point1);
    }
}
