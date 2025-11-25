using Journey.Application.Interfaces;
using Journey.Domain.Entities;
using Journey.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Journey.Infrastructure.EventHandlers;

/// <remarks>
/// Excluded from code coverage: MediatR event handler for domain events.
/// Event handling logic is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "MediatR event handler - event handling tested via integration tests.")]
public sealed class MonthlyDistanceReadModelEventHandler :
    INotificationHandler<JourneyCreated>,
    INotificationHandler<JourneyUpdated>,
    INotificationHandler<JourneyDeleted>
{
    private readonly IJourneyRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MonthlyDistanceReadModelEventHandler> _logger;

    public MonthlyDistanceReadModelEventHandler(
        IJourneyRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<MonthlyDistanceReadModelEventHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(JourneyCreated notification, CancellationToken cancellationToken)
    {
        var year = notification.StartTime.Year;
        var month = notification.StartTime.Month;

        var readModel = await _repository.GetMonthlyDistanceReadModelAsync(
            notification.UserId,
            year,
            month,
            cancellationToken);

        if (readModel is null)
        {
            readModel = new MonthlyDistanceReadModel(notification.UserId, year, month);
            await _repository.AddMonthlyDistanceReadModelAsync(readModel, cancellationToken);
        }

        readModel.AddDistance(notification.DistanceKm);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated monthly distance read model for user {UserId}, {Year}-{Month}: {TotalDistance} km",
            notification.UserId,
            year,
            month,
            readModel.TotalDistanceKm);
    }

    public async Task Handle(JourneyUpdated notification, CancellationToken cancellationToken)
    {
        var journey = await _repository.GetByIdAsync(notification.JourneyId, cancellationToken);
        if (journey is null) return;

        var oldYear = journey.StartTime.Year;
        var oldMonth = journey.StartTime.Month;
        var newYear = notification.StartTime.Year;
        var newMonth = notification.StartTime.Month;

        if (oldYear == newYear && oldMonth == newMonth)
        {
            var readModel = await _repository.GetMonthlyDistanceReadModelAsync(
                notification.UserId,
                newYear,
                newMonth,
                cancellationToken);

            if (readModel is not null)
            {
                readModel.UpdateDistance(journey.DistanceKm.Value, notification.DistanceKm);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            var oldReadModel = await _repository.GetMonthlyDistanceReadModelAsync(
                notification.UserId,
                oldYear,
                oldMonth,
                cancellationToken);

            if (oldReadModel is not null)
            {
                oldReadModel.RemoveDistance(journey.DistanceKm.Value);
            }

            var newReadModel = await _repository.GetMonthlyDistanceReadModelAsync(
                notification.UserId,
                newYear,
                newMonth,
                cancellationToken);

            if (newReadModel is null)
            {
                newReadModel = new MonthlyDistanceReadModel(notification.UserId, newYear, newMonth);
                await _repository.AddMonthlyDistanceReadModelAsync(newReadModel, cancellationToken);
            }

            newReadModel.AddDistance(notification.DistanceKm);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Updated monthly distance read model for journey {JourneyId}",
            notification.JourneyId);
    }

    public async Task Handle(JourneyDeleted notification, CancellationToken cancellationToken)
    {
        var year = notification.StartTime.Year;
        var month = notification.StartTime.Month;

        var readModel = await _repository.GetMonthlyDistanceReadModelAsync(
            notification.UserId,
            year,
            month,
            cancellationToken);

        if (readModel is not null)
        {
            readModel.RemoveDistance(notification.DistanceKm);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Updated monthly distance read model after journey {JourneyId} deletion for user {UserId}, {Year}-{Month}",
                notification.JourneyId,
                notification.UserId,
                year,
                month);
        }
    }
}

