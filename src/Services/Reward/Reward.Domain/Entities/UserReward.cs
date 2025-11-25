namespace Reward.Domain.Entities;

public sealed class UserReward
{
    private UserReward()
    {
    }

    public UserReward(string userId, DateTime date, decimal totalDistanceKm, int points)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Date = date.Date;
        TotalDistanceKm = totalDistanceKm;
        Points = points;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public DateTime Date { get; private set; }
    public decimal TotalDistanceKm { get; private set; }
    public int Points { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    public void AddDistance(decimal distanceKm, int additionalPoints)
    {
        TotalDistanceKm += distanceKm;
        Points += additionalPoints;
    }
}

