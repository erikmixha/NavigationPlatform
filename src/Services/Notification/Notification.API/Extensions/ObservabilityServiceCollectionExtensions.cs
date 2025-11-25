using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter.Prometheus;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Notification.API.Extensions;

/// <summary>
/// Extension methods for configuring observability services (OpenTelemetry).
/// </summary>
/// <remarks>
/// Excluded from code coverage: Extension methods for service configuration.
/// Configuration is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Extension methods for service configuration. Tested via integration tests.")]
public static class ObservabilityServiceCollectionExtensions
{
    /// <summary>
    /// Adds OpenTelemetry tracing and metrics with Jaeger and Prometheus exporters.
    /// </summary>
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("NotificationService"))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddJaegerExporter(options =>
                    {
                        options.AgentHost = configuration["Jaeger:Host"] ?? "localhost";
                        var portString = configuration["Jaeger:Port"];
                        options.AgentPort = !string.IsNullOrEmpty(portString) && int.TryParse(portString, out var port) 
                            ? port 
                            : 6831;
                    });
            })
            .WithMetrics(builder =>
            {
                builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("NotificationService"))
                    .AddAspNetCoreInstrumentation()
                    .AddPrometheusExporter();
            });

        return services;
    }
}

