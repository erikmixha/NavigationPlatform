using Journey.Application.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Messaging.Events;

namespace Journey.Infrastructure.Consumers;

/// <remarks>
/// Excluded from code coverage: MassTransit consumer for message processing.
/// Message handling logic is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "MassTransit consumer - message handling tested via integration tests.")]
public sealed class DailyGoalAchievedConsumer : IConsumer<DailyGoalAchievedEvent>
{
    private readonly IJourneyRepository _journeyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DailyGoalAchievedConsumer> _logger;

    public DailyGoalAchievedConsumer(
        IJourneyRepository journeyRepository,
        IUnitOfWork unitOfWork,
        ILogger<DailyGoalAchievedConsumer> logger)
    {
        _journeyRepository = journeyRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DailyGoalAchievedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Processing daily goal achieved event for user {UserId} on {Date}",
            message.UserId,
            message.Date);

        // Get all journeys for this user on this day
        var journeys = await _journeyRepository.GetByUserIdAndDateAsync(
            message.UserId,
            message.Date,
            context.CancellationToken);

        if (!journeys.Any())
        {
            _logger.LogWarning(
                "No journeys found for user {UserId} on {Date}",
                message.UserId,
                message.Date);
            return;
        }

        // Mark all journeys on this day as having achieved the daily goal
        foreach (var journey in journeys)
        {
            journey.SetDailyGoalAchieved();
        }

        await _unitOfWork.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation(
            "Marked {Count} journeys for user {UserId} on {Date} as daily goal achieved",
            journeys.Count(),
            message.UserId,
            message.Date);
    }
}

