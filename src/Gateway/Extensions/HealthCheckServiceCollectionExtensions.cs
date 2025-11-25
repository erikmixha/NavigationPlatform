using Microsoft.Extensions.DependencyInjection;

namespace Gateway.Extensions;

/// <summary>
/// Extension methods for configuring health check services.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Extension methods for service configuration.
/// Configuration is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Extension methods for service configuration. Tested via integration tests.")]
public static class HealthCheckServiceCollectionExtensions
{
    /// <summary>
    /// Adds health check services.
    /// </summary>
    public static IServiceCollection AddHealthCheckServices(this IServiceCollection services)
    {
        services.AddHealthChecks();
        return services;
    }
}

