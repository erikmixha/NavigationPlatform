using System.Diagnostics.CodeAnalysis;

namespace Shared.Messaging.Events;

/// <summary>
/// Event published when a user's status is changed (e.g., Active to Suspended).
/// </summary>
/// <remarks>
/// Excluded from code coverage: Simple event DTO.
/// Tested via integration tests.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Simple event DTO. Tested via integration tests.")]
public sealed record UserStatusChangedEvent
{
    /// <summary>
    /// Gets the identifier of the user whose status was changed.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the previous status of the user.
    /// </summary>
    public string PreviousStatus { get; init; } = string.Empty;

    /// <summary>
    /// Gets the new status of the user.
    /// </summary>
    public string NewStatus { get; init; } = string.Empty;

    /// <summary>
    /// Gets the identifier of the user who made the status change.
    /// </summary>
    public string ChangedByUserId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the UTC date and time when the event occurred.
    /// </summary>
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}
