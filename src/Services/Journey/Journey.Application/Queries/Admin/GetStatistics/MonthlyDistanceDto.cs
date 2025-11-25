namespace Journey.Application.Queries.Admin.GetStatistics;

public sealed class MonthlyDistanceDto
{
    public string UserId { get; init; } = string.Empty;
    public int Year { get; init; }
    public int Month { get; init; }
    public decimal TotalDistanceKm { get; init; }
}

