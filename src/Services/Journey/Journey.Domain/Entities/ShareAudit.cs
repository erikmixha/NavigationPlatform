namespace Journey.Domain.Entities;

/// <remarks>
/// Excluded from code coverage: Audit entity class with minimal logic.
/// Tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Audit entity class. Tested via integration tests.")]
public sealed class ShareAudit
{
    private ShareAudit()
    {
    }

    public ShareAudit(Guid journeyId, string action, string performedByUserId, string? targetUserId = null)
    {
        Id = Guid.NewGuid();
        JourneyId = journeyId;
        Action = action;
        PerformedByUserId = performedByUserId;
        TargetUserId = targetUserId;
        Timestamp = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid JourneyId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string PerformedByUserId { get; private set; } = string.Empty;
    public string? TargetUserId { get; private set; }
    public DateTime Timestamp { get; private set; }
}

