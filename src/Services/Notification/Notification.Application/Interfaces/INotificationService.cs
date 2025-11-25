namespace Notification.Application.Interfaces;

/// <summary>
/// Service interface for sending notifications via SignalR and email.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Attempts to send a notification via SignalR. Returns false if SignalR is unavailable.
    /// </summary>
    Task<bool> TrySendSignalRNotificationAsync(string userId, object notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email notification as a fallback when SignalR is unavailable.
    /// </summary>
    Task SendEmailNotificationAsync(string userId, string notificationType, string subject, string body, CancellationToken cancellationToken = default);
}

