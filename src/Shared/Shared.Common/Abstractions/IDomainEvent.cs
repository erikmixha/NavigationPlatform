namespace Shared.Common.Abstractions;

/// <summary>
/// Represents a domain event that occurred in the system.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier of the event.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the UTC date and time when the event occurred.
    /// </summary>
    DateTime OccurredOnUtc { get; }
}
