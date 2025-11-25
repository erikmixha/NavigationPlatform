using MassTransit;
using Notification.Application.Interfaces;
using Notification.Domain.Entities;
using Shared.Messaging.Events;
using NotificationEntity = Notification.Domain.Entities.Notification;

namespace Notification.API.Consumers;

/// <remarks>
/// Excluded from code coverage: MassTransit consumer for message processing.
/// Message handling logic is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "MassTransit consumer - message handling tested via integration tests.")]
public sealed class JourneyDeletedConsumer : IConsumer<JourneyDeletedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<JourneyDeletedConsumer> _logger;

    public JourneyDeletedConsumer(
        INotificationService notificationService,
        INotificationRepository notificationRepository,
        ILogger<JourneyDeletedConsumer> logger)
    {
        _notificationService = notificationService;
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<JourneyDeletedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Journey deleted event received for JourneyId: {JourneyId}, FavoritingUsers: {FavoritingUserIds}",
            message.JourneyId,
            string.Join(",", message.FavoritingUserIds));

        if (message.FavoritingUserIds == null || !message.FavoritingUserIds.Any())
        {
            _logger.LogInformation("No favoriting users for journey {JourneyId}, skipping notification", message.JourneyId);
            return;
        }

        var title = $"Journey Deleted: {message.StartLocation} to {message.ArrivalLocation}";
        var notificationMessage = $"A journey you favorited from {message.StartLocation} to {message.ArrivalLocation} has been deleted.";

        foreach (var userId in message.FavoritingUserIds)
        {
            var notification = new NotificationEntity(
                Guid.NewGuid(),
                userId,
                "JourneyDeleted",
                title,
                notificationMessage);

            await _notificationRepository.AddAsync(notification, context.CancellationToken);

            var signalRNotification = new
            {
                Type = "JourneyDeleted",
                JourneyId = message.JourneyId,
                Title = title,
                Message = notificationMessage,
                StartLocation = message.StartLocation,
                ArrivalLocation = message.ArrivalLocation,
                Timestamp = DateTime.UtcNow
            };

            var sent = await _notificationService.TrySendSignalRNotificationAsync(userId, signalRNotification);

            if (sent)
            {
                _logger.LogInformation(
                    "Sent journey deleted notification to user {UserId} for journey {JourneyId}",
                    userId,
                    message.JourneyId);
            }
            else
            {
                await _notificationService.SendEmailNotificationAsync(
                    userId,
                    "JourneyDeleted",
                    title,
                    notificationMessage);

                _logger.LogInformation(
                    "Sent email notification for offline user {UserId} for journey {JourneyId}",
                    userId,
                    message.JourneyId);
            }
        }

        await _notificationRepository.SaveChangesAsync(context.CancellationToken);
    }
}

