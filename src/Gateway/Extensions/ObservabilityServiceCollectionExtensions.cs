using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter.Prometheus;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Shared.Common.Configuration;

namespace Gateway.Extensions;

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
        var observabilityOptions = configuration.GetSection(ObservabilityOptions.SectionName)
            .Get<ObservabilityOptions>() ?? new ObservabilityOptions();

        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Gateway"))
                    .AddAspNetCoreInstrumentation()
                    .AddJaegerExporter(options =>
                    {
                        options.AgentHost = observabilityOptions.Jaeger.Host;
                        options.AgentPort = observabilityOptions.Jaeger.Port;
                    });
            })
            .WithMetrics(builder =>
            {
                builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Gateway"))
                    .AddAspNetCoreInstrumentation()
                    .AddPrometheusExporter();
            });

        return services;
    }
}
