namespace Notification.Domain.Entities;

/// <remarks>
/// Excluded from code coverage: Simple entity class with minimal business logic.
/// Tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Simple entity class. Tested via integration tests.")]
public sealed class Notification
{
    private Notification()
    {
    }

    public Notification(
        Guid id,
        string userId,
        string type,
        string title,
        string message,
        bool isRead = false)
    {
        Id = id;
        UserId = userId;
        Type = type;
        Title = title;
        Message = message;
        IsRead = isRead;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public bool IsRead { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? ReadOnUtc { get; private set; }

    public void MarkAsRead()
    {
        IsRead = true;
        ReadOnUtc = DateTime.UtcNow;
    }

    public void MarkAsUnread()
    {
        IsRead = false;
        ReadOnUtc = null;
    }
}

