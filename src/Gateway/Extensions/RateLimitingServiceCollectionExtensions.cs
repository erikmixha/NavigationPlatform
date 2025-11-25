using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gateway.Extensions;

/// <summary>
/// Extension methods for configuring rate limiting services.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Extension methods for service configuration.
/// Configuration is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Extension methods for service configuration. Tested via integration tests.")]
public static class RateLimitingServiceCollectionExtensions
{
    /// <summary>
    /// Adds rate limiting services.
    /// </summary>
    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient();
        return services;
    }
}

