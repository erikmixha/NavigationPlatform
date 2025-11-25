using System.Diagnostics.CodeAnalysis;

namespace Shared.Common.Configuration;

/// <summary>
/// Configuration settings for rate limiting.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Configuration class with simple properties.
/// Configuration is tested via integration tests.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Configuration class. Tested via integration tests.")]
public sealed class RateLimitSettings
{
    public const string SectionName = "RateLimit";

    /// <summary>
    /// Maximum number of login attempts allowed within the time window.
    /// Default: 5 attempts
    /// </summary>
    public int MaxLoginAttempts { get; set; } = 5;

    /// <summary>
    /// Time window in minutes for rate limiting.
    /// Default: 1 minute
    /// </summary>
    public int WindowMinutes { get; set; } = 1;
}
