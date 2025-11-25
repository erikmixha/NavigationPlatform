using System.Diagnostics.CodeAnalysis;
using Shared.Common.Abstractions;

namespace Shared.Common.Primitives;

/// <summary>
/// Base class for aggregate roots in Domain-Driven Design.
/// Provides functionality for raising and managing domain events.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Base class for aggregate roots.
/// Domain event management is tested via integration tests.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Base class for aggregate roots. Tested via integration tests.")]
public abstract class AggregateRoot : IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <inheritdoc />
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Raises a domain event that will be published after the aggregate is persisted.
    /// </summary>
    /// <param name="domainEvent">The domain event to raise.</param>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}
