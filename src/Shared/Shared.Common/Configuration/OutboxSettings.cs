using System.Diagnostics.CodeAnalysis;

namespace Shared.Common.Configuration;

/// <summary>
/// Configuration settings for the outbox pattern background service.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Configuration class with simple properties.
/// Configuration is tested via integration tests.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Configuration class. Tested via integration tests.")]
public sealed class OutboxSettings
{
    public const string SectionName = "Outbox";

    /// <summary>
    /// Interval in seconds between outbox message processing runs.
    /// Default: 10 seconds
    /// </summary>
    public int ProcessingIntervalSeconds { get; set; } = 10;

    /// <summary>
    /// Maximum number of messages to process in a single batch.
    /// Default: 20 messages
    /// </summary>
    public int BatchSize { get; set; } = 20;
}
