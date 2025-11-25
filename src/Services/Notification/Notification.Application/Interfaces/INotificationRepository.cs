using Notification.Domain.Entities;

namespace Notification.Application.Interfaces;

/// <summary>
/// Repository interface for notification data access operations.
/// </summary>
public interface INotificationRepository
{
    /// <summary>
    /// Gets a notification by its identifier.
    /// </summary>
    Task<Domain.Entities.Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notifications for a specific user, optionally filtered by read status.
    /// </summary>
    Task<List<Domain.Entities.Notification>> GetByUserIdAsync(string userId, bool? isRead = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of unread notifications for a user.
    /// </summary>
    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new notification.
    /// </summary>
    Task AddAsync(Domain.Entities.Notification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing notification.
    /// </summary>
    void Update(Domain.Entities.Notification notification);

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

