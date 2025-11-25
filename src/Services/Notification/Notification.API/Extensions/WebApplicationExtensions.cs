using Notification.Infrastructure.Extensions;
using Notification.Infrastructure.Hubs;
using OpenTelemetry.Exporter.Prometheus;
using Serilog;

namespace Notification.API.Extensions;

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
    /// Configures middleware pipeline including authentication, authorization, CORS, and logging.
    /// </summary>
    public static WebApplication ConfigureMiddleware(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseCors();

        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    /// <summary>
    /// Maps API endpoints including controllers, SignalR hubs, health checks, and metrics.
    /// </summary>
    public static WebApplication MapEndpoints(this WebApplication app)
    {
        app.MapControllers();
        app.MapHub<NotificationHub>("/hubs/notifications");
        app.MapHealthChecks("/healthz");
        app.MapPrometheusScrapingEndpoint("/metrics");

        return app;
    }
}
