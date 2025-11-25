namespace Shared.Common.Abstractions;

/// <summary>
/// Represents an aggregate root in Domain-Driven Design, capable of raising domain events.
/// </summary>
public interface IAggregateRoot
{
    /// <summary>
    /// Gets the collection of domain events raised by this aggregate root.
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Clears all domain events from this aggregate root.
    /// </summary>
    void ClearDomainEvents();
}
