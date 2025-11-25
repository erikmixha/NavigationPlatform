namespace Shared.Common.Configuration;

public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

    public JaegerSettings Jaeger { get; set; } = new();
    public PrometheusSettings Prometheus { get; set; } = new();
}

public sealed class JaegerSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 6831;
}

public sealed class PrometheusSettings
{
    public string MetricsPath { get; set; } = "/metrics";
    public bool Enabled { get; set; } = true;
}

