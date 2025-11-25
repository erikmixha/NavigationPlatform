using System.Diagnostics.CodeAnalysis;

namespace Gateway.Domain.Entities;

/// <remarks>
/// Excluded from code coverage: Audit entity class with minimal logic.
/// Tested via integration tests.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Audit entity class. Tested via integration tests.")]
public sealed class UserStatusAudit
{
    private UserStatusAudit()
    {
    }

    public UserStatusAudit(string userId, string previousStatus, string newStatus, string changedByUserId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        ChangedByUserId = changedByUserId;
        Timestamp = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string PreviousStatus { get; private set; } = string.Empty;
    public string NewStatus { get; private set; } = string.Empty;
    public string ChangedByUserId { get; private set; } = string.Empty;
    public DateTime Timestamp { get; private set; }
}

