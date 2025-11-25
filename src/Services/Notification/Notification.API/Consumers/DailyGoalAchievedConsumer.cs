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
public sealed class DailyGoalAchievedConsumer : IConsumer<DailyGoalAchievedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<DailyGoalAchievedConsumer> _logger;

    public DailyGoalAchievedConsumer(
        INotificationService notificationService,
        INotificationRepository notificationRepository,
        ILogger<DailyGoalAchievedConsumer> logger)
    {
        _notificationService = notificationService;
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DailyGoalAchievedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Daily goal achieved event received for UserId: {UserId}, TotalDistance: {TotalDistance} km",
            message.UserId,
            message.TotalDistanceKm);

        var title = $"Daily Goal Achieved! {message.GoalDistanceKm} km";
        var notificationMessage = $"Congratulations! You've achieved your daily goal of {message.GoalDistanceKm} km with a total of {message.TotalDistanceKm} km today!";

        var notification = new NotificationEntity(
            Guid.NewGuid(),
            message.UserId,
            "DailyGoalAchieved",
            title,
            notificationMessage);

        await _notificationRepository.AddAsync(notification, context.CancellationToken);
        await _notificationRepository.SaveChangesAsync(context.CancellationToken);

        var signalRNotification = new
        {
            Type = "DailyGoalAchieved",
            UserId = message.UserId,
            GoalDistanceKm = message.GoalDistanceKm,
            TotalDistanceKm = message.TotalDistanceKm,
            AchievedOnUtc = message.OccurredOnUtc,
            Timestamp = DateTime.UtcNow
        };

        var sent = await _notificationService.TrySendSignalRNotificationAsync(message.UserId, signalRNotification);

        if (sent)
        {
            _logger.LogInformation(
                "Sent daily goal achieved notification to user {UserId}",
                message.UserId);
        }
        else
        {
            await _notificationService.SendEmailNotificationAsync(
                message.UserId,
                "DailyGoalAchieved",
                title,
                notificationMessage);

            _logger.LogInformation(
                "Sent email notification for offline user {UserId} for daily goal achievement",
                message.UserId);
        }
    }
}

