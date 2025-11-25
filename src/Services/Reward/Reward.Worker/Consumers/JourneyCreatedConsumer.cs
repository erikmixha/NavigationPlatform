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
/// Consumer for handling journey created events and updating user rewards.
/// </summary>
/// <remarks>
/// Excluded from code coverage: MassTransit consumer for message processing.
/// Message handling logic is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "MassTransit consumer - message handling tested via integration tests.")]
public sealed class JourneyCreatedConsumer : IConsumer<JourneyCreatedEvent>
{
    private readonly RewardDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<JourneyCreatedConsumer> _logger;
    private readonly RewardSettings _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="JourneyCreatedConsumer"/> class.
    /// </summary>
    public JourneyCreatedConsumer(
        RewardDbContext context,
        IPublishEndpoint publishEndpoint,
        ILogger<JourneyCreatedConsumer> logger,
        IOptions<RewardSettings> settings)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _settings = settings.Value;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<JourneyCreatedEvent> context)
    {
        var message = context.Message;
        var date = message.StartTime.Date;

        _logger.LogInformation(
            "Processing journey created event for user {UserId}, distance {DistanceKm} km",
            message.UserId,
            message.DistanceKm);

        var existingReward = await _context.UserRewards
            .FirstOrDefaultAsync(ur => ur.UserId == message.UserId && ur.Date == date);

        var points = CalculatePoints(message.DistanceKm);
        var previousDistance = existingReward?.TotalDistanceKm ?? 0;

        UserReward userReward;
        if (existingReward is not null)
        {
            existingReward.AddDistance(message.DistanceKm, points);
            userReward = existingReward;
        }
        else
        {
            userReward = new UserReward(message.UserId, date, message.DistanceKm, points);
            await _context.UserRewards.AddAsync(userReward);
        }

        await _context.SaveChangesAsync();

        var updatedReward = await _context.UserRewards
            .AsNoTracking()
            .FirstOrDefaultAsync(ur => ur.UserId == message.UserId && ur.Date == date);
        var newTotalDistance = updatedReward?.TotalDistanceKm ?? message.DistanceKm;
        if (previousDistance < _settings.DailyGoalKm && newTotalDistance >= _settings.DailyGoalKm)
        {
            var goalEvent = new DailyGoalAchievedEvent
            {
                UserId = message.UserId,
                Date = date,
                TotalDistanceKm = newTotalDistance,
                GoalDistanceKm = _settings.DailyGoalKm,
                Points = points,
                OccurredOnUtc = DateTime.UtcNow
            };

            await _publishEndpoint.Publish(goalEvent);

            _logger.LogInformation(
                "User {UserId} achieved daily goal on {Date} with {TotalDistance} km",
                message.UserId,
                date,
                newTotalDistance);
        }
    }

    private int CalculatePoints(decimal distanceKm)
    {
        return (int)(distanceKm * _settings.PointsPerKm);
    }
}
