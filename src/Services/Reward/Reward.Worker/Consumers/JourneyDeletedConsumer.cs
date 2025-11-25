using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reward.Infrastructure.Persistence;
using Shared.Common.Configuration;
using Shared.Messaging.Events;

namespace Reward.Worker.Consumers;

/// <summary>
/// Consumer for handling journey deleted events and removing distance/points from user rewards.
/// </summary>
/// <remarks>
/// Excluded from code coverage: MassTransit consumer for message processing.
/// Message handling logic is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "MassTransit consumer - message handling tested via integration tests.")]
public sealed class JourneyDeletedConsumer : IConsumer<JourneyDeletedEvent>
{
    private readonly RewardDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<JourneyDeletedConsumer> _logger;
    private readonly RewardSettings _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="JourneyDeletedConsumer"/> class.
    /// </summary>
    public JourneyDeletedConsumer(
        RewardDbContext context,
        IPublishEndpoint publishEndpoint,
        ILogger<JourneyDeletedConsumer> logger,
        IOptions<RewardSettings> settings)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _settings = settings.Value;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<JourneyDeletedEvent> context)
    {
        var message = context.Message;
        var date = message.StartTime.Date;

        _logger.LogInformation(
            "Processing journey deleted event for user {UserId}, journey {JourneyId}, distance {DistanceKm} km on {Date}",
            message.UserId,
            message.JourneyId,
            message.DistanceKm,
            date);

        var existingReward = await _context.UserRewards
            .FirstOrDefaultAsync(ur => ur.UserId == message.UserId && ur.Date == date, context.CancellationToken);

        if (existingReward is null)
        {
            _logger.LogWarning(
                "No reward record found for user {UserId} on {Date} when deleting journey {JourneyId}",
                message.UserId,
                date,
                message.JourneyId);
            return;
        }

        var previousDistance = existingReward.TotalDistanceKm;
        var pointsToSubtract = CalculatePoints(message.DistanceKm);

        existingReward.AddDistance(-message.DistanceKm, -pointsToSubtract);

        await _context.SaveChangesAsync(context.CancellationToken);

        var newTotalDistance = existingReward.TotalDistanceKm;

        _logger.LogInformation(
            "Updated reward for user {UserId} on {Date}: {PreviousDistance} km -> {NewDistance} km (deleted {DeletedDistance} km)",
            message.UserId,
            date,
            previousDistance,
            newTotalDistance,
            message.DistanceKm);
    }

    private int CalculatePoints(decimal distanceKm)
    {
        return (int)(distanceKm * _settings.PointsPerKm);
    }
}
