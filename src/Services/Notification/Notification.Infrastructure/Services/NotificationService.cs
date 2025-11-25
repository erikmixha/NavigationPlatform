using System.Collections.Concurrent;
using System.Net.Mail;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Application.Interfaces;
using Notification.Infrastructure.Hubs;
using Shared.Common.Configuration;

namespace Notification.Infrastructure.Services;

/// <remarks>
/// Excluded from code coverage: Infrastructure service for SignalR and email notifications.
/// Notification delivery is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Infrastructure notification service. Tested via integration tests.")]
public sealed class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly SmtpOptions _smtpOptions;
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly ILogger<NotificationService> _logger;
    private static readonly ConcurrentDictionary<string, HashSet<string>> ConnectedUsers = new();

    public NotificationService(
        IHubContext<NotificationHub> hubContext,
        IOptions<SmtpOptions> smtpOptions,
        IKeycloakUserService keycloakUserService,
        ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _smtpOptions = smtpOptions.Value;
        _keycloakUserService = keycloakUserService;
        _logger = logger;
    }

    public static void AddConnection(string userId, string connectionId)
    {
        ConnectedUsers.AddOrUpdate(
            userId,
            new HashSet<string> { connectionId },
            (key, existing) =>
            {
                existing.Add(connectionId);
                return existing;
            });
    }

    public static void RemoveConnection(string userId, string connectionId)
    {
        if (ConnectedUsers.TryGetValue(userId, out var connections))
        {
            connections.Remove(connectionId);
            if (connections.Count == 0)
            {
                ConnectedUsers.TryRemove(userId, out _);
            }
        }
    }

    public static bool IsUserConnected(string userId)
    {
        return ConnectedUsers.TryGetValue(userId, out var connections) && connections.Count > 0;
    }

    public async Task<bool> TrySendSignalRNotificationAsync(string userId, object notification, CancellationToken cancellationToken = default)
    {
        if (!IsUserConnected(userId))
        {
            _logger.LogInformation("User {UserId} is not connected to SignalR, will send email", userId);
            return false;
        }

        try
        {
            await _hubContext.Clients
                .User(userId)
                .SendAsync("ReceiveNotification", notification, cancellationToken);

            _logger.LogInformation("Successfully sent SignalR notification to user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to send SignalR notification to user {UserId}, will queue email",
                userId);
            return false;
        }
    }

    public async Task SendEmailNotificationAsync(string userId, string notificationType, string subject, string body, CancellationToken cancellationToken = default)
    {
        var userEmail = await _keycloakUserService.GetUserEmailAsync(userId, cancellationToken);
        
        if (string.IsNullOrEmpty(userEmail))
        {
            _logger.LogWarning("Cannot send email for user {UserId}: email not found in Keycloak", userId);
            return;
        }

        try
        {
            using var smtpClient = new SmtpClient(_smtpOptions.Host, _smtpOptions.Port)
            {
                EnableSsl = _smtpOptions.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!string.IsNullOrEmpty(_smtpOptions.Username) && !string.IsNullOrEmpty(_smtpOptions.Password))
            {
                smtpClient.Credentials = new System.Net.NetworkCredential(_smtpOptions.Username, _smtpOptions.Password);
            }

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_smtpOptions.FromEmail, _smtpOptions.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };

            mailMessage.To.Add(userEmail);

            await smtpClient.SendMailAsync(mailMessage, cancellationToken);

            _logger.LogInformation(
                "Sent email notification to {Email} for user {UserId}, type: {NotificationType}",
                userEmail,
                userId,
                notificationType);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send email notification to user {UserId}, type: {NotificationType}",
                userId,
                notificationType);
        }
    }
}

