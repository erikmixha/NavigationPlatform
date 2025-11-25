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
public sealed class JourneyUpdatedConsumer : IConsumer<JourneyUpdatedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<JourneyUpdatedConsumer> _logger;

    public JourneyUpdatedConsumer(
        INotificationService notificationService,
        INotificationRepository notificationRepository,
        ILogger<JourneyUpdatedConsumer> logger)
    {
        _notificationService = notificationService;
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<JourneyUpdatedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Journey updated event received for JourneyId: {JourneyId}, FavoritingUsers: {FavoritingUserIds}",
            message.JourneyId,
            string.Join(",", message.FavoritingUserIds));

        if (message.FavoritingUserIds == null || !message.FavoritingUserIds.Any())
        {
            _logger.LogInformation("No favoriting users for journey {JourneyId}, skipping notification", message.JourneyId);
            return;
        }

        var title = $"Journey Updated: {message.StartLocation} to {message.ArrivalLocation}";
        var notificationMessage = $"A journey you favorited has been updated. Distance: {message.DistanceKm} km, Transport: {message.TransportType}";

        foreach (var userId in message.FavoritingUserIds)
        {
            var notification = new NotificationEntity(
                Guid.NewGuid(),
                userId,
                "JourneyUpdated",
                title,
                notificationMessage);

            await _notificationRepository.AddAsync(notification, context.CancellationToken);

            var signalRNotification = new
            {
                Type = "JourneyUpdated",
                JourneyId = message.JourneyId,
                Title = title,
                Message = notificationMessage,
                StartLocation = message.StartLocation,
                ArrivalLocation = message.ArrivalLocation,
                StartTime = message.StartTime,
                ArrivalTime = message.ArrivalTime,
                TransportType = message.TransportType,
                DistanceKm = message.DistanceKm,
                Timestamp = DateTime.UtcNow
            };

            var sent = await _notificationService.TrySendSignalRNotificationAsync(userId, signalRNotification);

            if (sent)
            {
                _logger.LogInformation(
                    "Sent journey updated notification to user {UserId} for journey {JourneyId}",
                    userId,
                    message.JourneyId);
            }
            else
            {
                await _notificationService.SendEmailNotificationAsync(
                    userId,
                    "JourneyUpdated",
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

