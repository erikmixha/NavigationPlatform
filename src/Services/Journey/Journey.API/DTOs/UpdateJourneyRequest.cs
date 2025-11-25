namespace Journey.API.DTOs;

public sealed record UpdateJourneyRequest
{
    public string StartLocation { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public string ArrivalLocation { get; init; } = string.Empty;
    public DateTime ArrivalTime { get; init; }
    public string TransportType { get; init; } = string.Empty;
    public decimal DistanceKm { get; init; }
}

