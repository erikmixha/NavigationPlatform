using System.Diagnostics.CodeAnalysis;

namespace Shared.Messaging.Events;

/// <summary>
/// Event published when a user achieves their daily distance goal.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Simple event DTO.
/// Tested via integration tests.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Simple event DTO. Tested via integration tests.")]
public sealed record DailyGoalAchievedEvent
{
    /// <summary>
    /// Gets the identifier of the user who achieved the goal.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the date when the goal was achieved.
    /// </summary>
    public DateTime Date { get; init; }

    /// <summary>
    /// Gets the total distance traveled in kilometers on this date.
    /// </summary>
    public decimal TotalDistanceKm { get; init; }

    /// <summary>
    /// Gets the goal distance in kilometers that was set for this date.
    /// </summary>
    public decimal GoalDistanceKm { get; init; }

    /// <summary>
    /// Gets the reward points earned for achieving the goal.
    /// </summary>
    public int Points { get; init; }

    /// <summary>
    /// Gets the UTC date and time when the event occurred.
    /// </summary>
    public DateTime OccurredOnUtc { get; init; }
}
