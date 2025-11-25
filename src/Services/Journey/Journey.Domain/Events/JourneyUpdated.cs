using Journey.Domain.Enums;
using Shared.Common.Primitives;

namespace Journey.Domain.Events;

public sealed record JourneyUpdated : DomainEvent
{
    public Guid JourneyId { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string StartLocation { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public string ArrivalLocation { get; init; } = string.Empty;
    public DateTime ArrivalTime { get; init; }
    public TransportType TransportType { get; init; }
    public decimal DistanceKm { get; init; }
    public decimal OldDistanceKm { get; init; }
    public DateTime OldStartTime { get; init; }
}

