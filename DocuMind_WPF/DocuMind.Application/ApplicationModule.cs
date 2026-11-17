using DocuMind.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DocuMind.Application;

public static class ApplicationModule
{
    /// <summary>Registers the use-case services. Their ports (interfaces) are supplied by an outer layer.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<RagOrchestratorService>();
        services.AddScoped<QuizService>();
        return services;
    }
}
