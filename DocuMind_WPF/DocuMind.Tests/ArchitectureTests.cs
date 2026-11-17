using System.Reflection;
using DocuMind.Application;
using DocuMind.Application.Interfaces;
using DocuMind.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DocuMind.Tests;

/// <summary>Guards the Clean Architecture dependency rule: references only point inwards.</summary>
public class ArchitectureTests
{
    private static readonly Assembly Domain = typeof(DocuMind.Domain.Entities.Workspace).Assembly;
    private static readonly Assembly App = typeof(ApplicationModule).Assembly;
    private static readonly Assembly Infra = typeof(InfrastructureModule).Assembly;
    private static readonly Assembly Wpf = typeof(DocuMind.WPF.App).Assembly;

    private static string[] References(Assembly a) => a.GetReferencedAssemblies().Select(r => r.Name!).ToArray();

    [Fact]
    public void DomainDependsOnNothingFromTheSolutionOrThirdParties()
    {
        Assert.DoesNotContain(References(Domain), n => n.StartsWith("DocuMind.") || n.StartsWith("Microsoft.EntityFrameworkCore"));
    }

    [Fact]
    public void ApplicationDependsOnlyOnDomain()
    {
        var refs = References(App);
        Assert.Contains(Domain.GetName().Name, refs);
        Assert.DoesNotContain(refs, n => n.StartsWith("DocuMind.Infrastructure") || n.StartsWith("DocuMind.WPF")
            || n.StartsWith("Microsoft.EntityFrameworkCore") || n.StartsWith("Npgsql") || n.StartsWith("UglyToad") || n.StartsWith("PresentationFramework"));
    }

    [Fact]
    public void InfrastructureNeverReferencesPresentation()
    {
        Assert.DoesNotContain(References(Infra), n => n.StartsWith("DocuMind.WPF") || n.StartsWith("PresentationFramework"));
    }

    [Fact]
    public void PresentationDoesNotTalkToEfCoreOrDatabaseDriversDirectly()
    {
        Assert.DoesNotContain(References(Wpf), n => n.StartsWith("Microsoft.EntityFrameworkCore") || n.StartsWith("Npgsql") || n.StartsWith("Microsoft.Data.Sqlite"));
    }

    [Fact]
    public void LayerModulesRegisterEveryApplicationPort()
    {
        var services = new ServiceCollection().AddApplication().AddInfrastructure();
        var registered = services.Select(d => d.ServiceType).ToHashSet();
        var ports = App.GetTypes().Where(t => t.IsInterface && t.Namespace == "DocuMind.Application.Interfaces"
            && !t.IsGenericTypeDefinition && t != typeof(IChatCompletionService) && t != typeof(IEmbeddingService));
        Assert.All(ports, p => Assert.Contains(p, registered));
        Assert.Contains(typeof(IRepository<>), registered);
    }
}
