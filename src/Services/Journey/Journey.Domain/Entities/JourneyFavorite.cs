namespace Journey.Domain.Entities;

/// <remarks>
/// Excluded from code coverage: Simple entity class with minimal logic.
/// Tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Simple entity class. Tested via integration tests.")]
public sealed class JourneyFavorite
{
    private JourneyFavorite()
    {
    }

    public JourneyFavorite(Guid journeyId, string userId)
    {
        Id = Guid.NewGuid();
        JourneyId = journeyId;
        UserId = userId;
        FavoritedOnUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid JourneyId { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public DateTime FavoritedOnUtc { get; private set; }

    public Journey Journey { get; private set; } = null!;
}

