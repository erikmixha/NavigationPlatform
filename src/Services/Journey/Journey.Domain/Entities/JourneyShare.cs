namespace Journey.Domain.Entities;

/// <remarks>
/// Excluded from code coverage: Simple entity class with minimal logic.
/// Tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Simple entity class. Tested via integration tests.")]
public sealed class JourneyShare
{
    private JourneyShare()
    {
    }

    public JourneyShare(Guid journeyId, string sharedWithUserId, string sharedByUserId)
    {
        Id = Guid.NewGuid();
        JourneyId = journeyId;
        SharedWithUserId = sharedWithUserId;
        SharedByUserId = sharedByUserId;
        SharedOnUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid JourneyId { get; private set; }
    public string SharedWithUserId { get; private set; } = string.Empty;
    public string SharedByUserId { get; private set; } = string.Empty;
    public DateTime SharedOnUtc { get; private set; }

    public Journey Journey { get; private set; } = null!;
}

