using System.Diagnostics.CodeAnalysis;

namespace Shared.Common.Configuration;

/// <remarks>
/// Excluded from code coverage: Configuration class with simple properties.
/// Configuration is tested via integration tests.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Configuration class. Tested via integration tests.")]
public sealed class RewardSettings
{
    public const string SectionName = "Reward";

    public decimal DailyGoalKm { get; set; } = 20.0m;
    public int PointsPerKm { get; set; } = 10;
}

