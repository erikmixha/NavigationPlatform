using MediatR;
using Shared.Common.Result;

namespace Journey.Application.Queries.Admin.GetStatistics;

public sealed record GetStatisticsQuery : IRequest<Result<StatisticsDto>>;

public sealed record StatisticsDto
{
    public int TotalJourneys { get; init; }
    public int TotalUsers { get; init; }
    public decimal TotalDistanceKm { get; init; }
    public decimal AverageDistanceKm { get; init; }
    public Dictionary<string, int> JourneysByTransportType { get; init; } = new();
    public DateTime GeneratedOnUtc { get; init; }
}

