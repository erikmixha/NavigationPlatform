namespace Notification.Application.Abstractions;

public interface INotificationService
{
    Task<bool> TrySendSignalRNotificationAsync(string userId, object notification, CancellationToken cancellationToken = default);
    Task SendEmailNotificationAsync(string userId, string notificationType, string subject, string body, CancellationToken cancellationToken = default);
}

