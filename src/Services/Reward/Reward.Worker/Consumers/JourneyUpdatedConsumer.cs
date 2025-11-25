using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reward.Domain.Entities;
using Reward.Infrastructure.Persistence;
using Shared.Common.Configuration;
using Shared.Messaging.Events;

namespace Reward.Worker.Consumers;

/// <summary>
/// Consumer for handling journey updated events and adjusting user rewards accordingly.
/// </summary>
/// <remarks>
/// Excluded from code coverage: MassTransit consumer for message processing.
/// Message handling logic is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "MassTransit consumer - message handling tested via integration tests.")]
public sealed class JourneyUpdatedConsumer : IConsumer<JourneyUpdatedEvent>
{
    private readonly RewardDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<JourneyUpdatedConsumer> _logger;
    private readonly RewardSettings _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="JourneyUpdatedConsumer"/> class.
    /// </summary>
    public JourneyUpdatedConsumer(
        RewardDbContext context,
        IPublishEndpoint publishEndpoint,
        ILogger<JourneyUpdatedConsumer> logger,
        IOptions<RewardSettings> settings)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _settings = settings.Value;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<JourneyUpdatedEvent> context)
    {
        var message = context.Message;
        var oldDate = message.OldStartTime.Date;
        var newDate = message.StartTime.Date;

        _logger.LogInformation(
            "Processing journey updated event for user {UserId}, journey {JourneyId}. Old: {OldDistance} km on {OldDate}, New: {NewDistance} km on {NewDate}",
            message.UserId,
            message.JourneyId,
            message.OldDistanceKm,
            oldDate,
            message.DistanceKm,
            newDate);

        if (oldDate != newDate)
        {
            await UpdateRewardForDate(
                message.UserId,
                oldDate,
                -message.OldDistanceKm,
                -CalculatePoints(message.OldDistanceKm),
                context.CancellationToken);

            await UpdateRewardForDate(
                message.UserId,
                newDate,
                message.DistanceKm,
                CalculatePoints(message.DistanceKm),
                context.CancellationToken);
        }
        else if (message.OldDistanceKm != message.DistanceKm)
        {
            var distanceDiff = message.DistanceKm - message.OldDistanceKm;
            var pointsDiff = CalculatePoints(message.DistanceKm) - CalculatePoints(message.OldDistanceKm);

            await UpdateRewardForDate(
                message.UserId,
                newDate,
                distanceDiff,
                pointsDiff,
                context.CancellationToken);
        }
    }

    private async Task UpdateRewardForDate(
        string userId,
        DateTime date,
        decimal distanceChange,
        int pointsChange,
        CancellationToken cancellationToken)
    {
        var existingReward = await _context.UserRewards
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.Date == date, cancellationToken);

        var previousDistance = existingReward?.TotalDistanceKm ?? 0;

        if (existingReward is not null)
        {
            existingReward.AddDistance(distanceChange, pointsChange);
        }
        else if (distanceChange > 0)
        {
            var userReward = new UserReward(userId, date, distanceChange, pointsChange);
            await _context.UserRewards.AddAsync(userReward, cancellationToken);
        }
        else
        {
            _logger.LogWarning(
                "Cannot subtract distance {Distance} km for user {UserId} on {Date} - no reward record exists",
                distanceChange,
                userId,
                date);
            return;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var newTotalDistance = existingReward?.TotalDistanceKm ?? (distanceChange > 0 ? distanceChange : 0);

        var wasAchieved = previousDistance >= _settings.DailyGoalKm;
        var isAchieved = newTotalDistance >= _settings.DailyGoalKm;

        if (!wasAchieved && isAchieved)
        {
            var goalEvent = new DailyGoalAchievedEvent
            {
                UserId = userId,
                Date = date,
                TotalDistanceKm = newTotalDistance,
                GoalDistanceKm = _settings.DailyGoalKm,
                Points = pointsChange,
                OccurredOnUtc = DateTime.UtcNow
            };

            await _publishEndpoint.Publish(goalEvent, cancellationToken);

            _logger.LogInformation(
                "User {UserId} achieved daily goal on {Date} with {TotalDistance} km (after journey update)",
                userId,
                date,
                newTotalDistance);
        }
    }

    private int CalculatePoints(decimal distanceKm)
    {
        return (int)(distanceKm * _settings.PointsPerKm);
    }
}
