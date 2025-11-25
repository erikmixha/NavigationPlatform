using Journey.API.Filters;
using Microsoft.Extensions.DependencyInjection;
using Shared.Common.Behaviors;

namespace Journey.API.Extensions;

/// <summary>
/// Main extension methods for configuring Journey API services.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Extension methods for service configuration.
/// Configuration is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Extension methods for service configuration. Tested via integration tests.")]
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds API filters including validation exception handling.
    /// </summary>
    public static IServiceCollection AddApiFilters(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add<ValidationExceptionFilter>();
        });

        return services;
    }

    /// <summary>
    /// Adds health check services with PostgreSQL database check.
    /// </summary>
    public static IServiceCollection AddHealthCheckServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("JourneyDb");
        var healthChecksBuilder = services.AddHealthChecks();
        
        if (!string.IsNullOrEmpty(connectionString))
        {
            healthChecksBuilder.AddNpgSql(
                connectionString,
                name: "journey-db",
                tags: new[] { "db", "postgres" });
        }

        return services;
    }

    /// <summary>
    /// Adds MediatR pipeline behaviors including validation.
    /// </summary>
    public static IServiceCollection AddMediatRPipeline(this IServiceCollection services)
    {
        services.AddScoped(typeof(MediatR.IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        return services;
    }
}
