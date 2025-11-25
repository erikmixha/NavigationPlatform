using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Notification.Infrastructure.Hubs;

namespace Notification.API.Extensions;

/// <summary>
/// Main extension methods for configuring Notification API services.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Extension methods for service configuration.
/// Configuration is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Extension methods for service configuration. Tested via integration tests.")]
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SignalR services.
    /// </summary>
    public static IServiceCollection AddSignalRServices(this IServiceCollection services)
    {
        services.AddSignalR();
        return services;
    }

    /// <summary>
    /// Adds health check services.
    /// </summary>
    public static IServiceCollection AddHealthCheckServices(this IServiceCollection services)
    {
        services.AddHealthChecks();
        return services;
    }
}
