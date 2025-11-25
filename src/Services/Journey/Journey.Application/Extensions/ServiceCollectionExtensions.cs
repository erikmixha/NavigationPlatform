using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Journey.Application.Extensions;

/// <summary>
/// Extension methods for configuring the Application layer services.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Extension methods for service configuration.
/// Configuration is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Extension methods for service configuration. Tested via integration tests.")]
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Application layer services including MediatR and FluentValidation.
    /// </summary>
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        var applicationAssembly = typeof(ServiceCollectionExtensions).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(applicationAssembly);
        });
        services.AddValidatorsFromAssembly(applicationAssembly);

        return services;
    }
}
