using System.Diagnostics.CodeAnalysis;
using MediatR;
using Shared.Common.Abstractions;

namespace Shared.Common.Primitives;

/// <summary>
/// Base class for domain events that can be published via MediatR.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Base class for domain events.
/// Domain events are tested via integration tests.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Base class for domain events. Tested via integration tests.")]
public abstract record DomainEvent : IDomainEvent, INotification
{
    /// <inheritdoc />
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}
