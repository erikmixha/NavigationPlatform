namespace Shared.Common.Abstractions;

/// <summary>
/// Represents an outbox message used in the outbox pattern for reliable event publishing.
/// </summary>
public interface IOutboxMessage
{
    /// <summary>
    /// Gets the unique identifier of the outbox message.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the type of the domain event stored in this outbox message.
    /// </summary>
    string EventType { get; }

    /// <summary>
    /// Gets the serialized payload of the domain event.
    /// </summary>
    string Payload { get; }

    /// <summary>
    /// Gets the UTC date and time when the event occurred.
    /// </summary>
    DateTime OccurredOnUtc { get; }

    /// <summary>
    /// Gets the UTC date and time when the message was processed, or null if not yet processed.
    /// </summary>
    DateTime? ProcessedOnUtc { get; }

    /// <summary>
    /// Gets the error message if processing failed, or null if processing succeeded or hasn't been attempted.
    /// </summary>
    string? Error { get; }
}
