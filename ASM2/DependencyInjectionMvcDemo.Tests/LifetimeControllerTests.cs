using DependencyInjectionMvcDemo.Controllers;
using DependencyInjectionMvcDemo.Interfaces;
using DependencyInjectionMvcDemo.Models;
using DependencyInjectionMvcDemo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjectionMvcDemo.Tests;

public sealed class LifetimeControllerTests
{
    [Fact]
    public void Index_ReturnsComparisonModel_ShowingTransientDiffersAndScopedSingletonMatchInRequest()
    {
        var services = new ServiceCollection();
        services.AddTransient<ITransientMarker, TransientMarker>();
        services.AddScoped<IScopedMarker, ScopedMarker>();
        services.AddSingleton<ISingletonMarker, SingletonMarker>();
        services.AddScoped<ILifetimeSnapshotService, LifetimeSnapshotService>();
        services.AddTransient<LifetimeController>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
        using var scope = provider.CreateScope();

        var controller = scope.ServiceProvider.GetRequiredService<LifetimeController>();
        var result = controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<LifetimeComparisonViewModel>(viewResult.Model);

        Assert.NotEqual(model.ControllerTransientId, model.SnapshotTransientId);
        Assert.Equal(model.ControllerScopedId, model.SnapshotScopedId);
        Assert.Equal(model.ControllerSingletonId, model.SnapshotSingletonId);
        Assert.False(model.IsTransientSameInRequest);
        Assert.True(model.IsScopedSameInRequest);
        Assert.True(model.IsSingletonSameInRequest);
    }
}
