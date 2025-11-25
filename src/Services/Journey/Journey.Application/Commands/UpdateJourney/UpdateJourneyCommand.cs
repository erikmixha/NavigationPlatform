using MediatR;
using Shared.Common.Result;

namespace Journey.Application.Commands.UpdateJourney;

public sealed record UpdateJourneyCommand : IRequest<Result>
{
    public Guid Id { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string StartLocation { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public string ArrivalLocation { get; init; } = string.Empty;
    public DateTime ArrivalTime { get; init; }
    public string TransportType { get; init; } = string.Empty;
    public decimal DistanceKm { get; init; }
}

