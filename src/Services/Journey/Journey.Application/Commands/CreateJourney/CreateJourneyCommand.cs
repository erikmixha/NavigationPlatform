using MediatR;
using Shared.Common.Result;

namespace Journey.Application.Commands.CreateJourney;

public sealed record CreateJourneyCommand : IRequest<Result<Guid>>
{
    public string UserId { get; init; } = string.Empty;
    public string StartLocation { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public string ArrivalLocation { get; init; } = string.Empty;
    public DateTime ArrivalTime { get; init; }
    public string TransportType { get; init; } = string.Empty;
    public decimal DistanceKm { get; init; }
}

