using System.Diagnostics.CodeAnalysis;

namespace Shared.Messaging.Events;

/// <summary>
/// Event published when a journey is updated.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Simple event DTO.
/// Tested via integration tests.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Simple event DTO. Tested via integration tests.")]
public sealed record JourneyUpdatedEvent
{
    /// <summary>
    /// Gets the unique identifier of the journey.
    /// </summary>
    public Guid JourneyId { get; init; }

    /// <summary>
    /// Gets the identifier of the user who owns the journey.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the starting location of the journey.
    /// </summary>
    public string StartLocation { get; init; } = string.Empty;

    /// <summary>
    /// Gets the start time of the journey.
    /// </summary>
    public DateTime StartTime { get; init; }

    /// <summary>
    /// Gets the arrival location of the journey.
    /// </summary>
    public string ArrivalLocation { get; init; } = string.Empty;

    /// <summary>
    /// Gets the arrival time of the journey.
    /// </summary>
    public DateTime ArrivalTime { get; init; }

    /// <summary>
    /// Gets the transport type used for the journey.
    /// </summary>
    public string TransportType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the new distance of the journey in kilometers.
    /// </summary>
    public decimal DistanceKm { get; init; }

    /// <summary>
    /// Gets the previous distance of the journey in kilometers.
    /// </summary>
    public decimal OldDistanceKm { get; init; }

    /// <summary>
    /// Gets the previous start time of the journey.
    /// </summary>
    public DateTime OldStartTime { get; init; }

    /// <summary>
    /// Gets the list of user IDs who have favorited this journey.
    /// </summary>
    public List<string> FavoritingUserIds { get; init; } = new();

    /// <summary>
    /// Gets the UTC date and time when the event occurred.
    /// </summary>
    public DateTime OccurredOnUtc { get; init; }
}
