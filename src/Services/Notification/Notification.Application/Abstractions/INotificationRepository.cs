using Notification.Domain.Entities;

namespace Notification.Application.Abstractions;

public interface INotificationRepository
{
    Task<Domain.Entities.Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Domain.Entities.Notification>> GetByUserIdAsync(string userId, bool? isRead = null, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync(Domain.Entities.Notification notification, CancellationToken cancellationToken = default);
    void Update(Domain.Entities.Notification notification);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

