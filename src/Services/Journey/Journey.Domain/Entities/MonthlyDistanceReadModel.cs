namespace Journey.Domain.Entities;

/// <remarks>
/// Excluded from code coverage: Read model entity class.
/// Tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Read model entity. Tested via integration tests.")]
public sealed class MonthlyDistanceReadModel
{
    private MonthlyDistanceReadModel()
    {
    }

    public MonthlyDistanceReadModel(string userId, int year, int month)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Year = year;
        Month = month;
        TotalDistanceKm = 0;
        JourneyCount = 0;
        LastUpdatedOnUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public int Year { get; private set; }
    public int Month { get; private set; }
    public decimal TotalDistanceKm { get; private set; }
    public int JourneyCount { get; private set; }
    public DateTime LastUpdatedOnUtc { get; private set; }

    public void AddDistance(decimal distanceKm)
    {
        TotalDistanceKm += distanceKm;
        JourneyCount++;
        LastUpdatedOnUtc = DateTime.UtcNow;
    }

    public void UpdateDistance(decimal oldDistanceKm, decimal newDistanceKm)
    {
        TotalDistanceKm = TotalDistanceKm - oldDistanceKm + newDistanceKm;
        LastUpdatedOnUtc = DateTime.UtcNow;
    }

    public void RemoveDistance(decimal distanceKm)
    {
        TotalDistanceKm = Math.Max(0, TotalDistanceKm - distanceKm);
        JourneyCount = Math.Max(0, JourneyCount - 1);
        LastUpdatedOnUtc = DateTime.UtcNow;
    }
}

