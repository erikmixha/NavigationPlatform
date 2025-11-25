using Journey.Infrastructure.Extensions;
using Shared.Common.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace Journey.API.Extensions;

/// <summary>
/// Extension methods for configuring the web application pipeline.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Extension methods for application configuration.
/// Configuration is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Extension methods for application configuration. Tested via integration tests.")]
public static class WebApplicationExtensions
{
    /// <summary>
    /// Configures middleware pipeline including authentication, authorization, and Swagger.
    /// </summary>
    public static WebApplication ConfigureMiddleware(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    /// <summary>
    /// Maps API endpoints including controllers, health checks, and metrics.
    /// </summary>
    public static WebApplication MapEndpoints(this WebApplication app)
    {
        app.MapControllers();
        app.MapHealthChecks("/healthz");
        app.MapHealthChecks("/readyz");
        app.MapPrometheusScrapingEndpoint("/metrics");

        return app;
    }

    /// <summary>
    /// Applies database migrations asynchronously.
    /// </summary>
    public static async Task<WebApplication> ApplyDatabaseMigrationsAsync(this WebApplication app)
    {
        await app.Services.ApplyMigrationsAsync();
        return app;
    }
}
