using Journey.Application.Interfaces;
using Journey.Domain.Enums;
using MediatR;
using Shared.Common.Result;

namespace Journey.Application.Commands.UpdateJourney;

/// <summary>
/// Handler for updating an existing journey.
/// </summary>
public sealed class UpdateJourneyCommandHandler : IRequestHandler<UpdateJourneyCommand, Result>
{
    private readonly IJourneyRepository _journeyRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateJourneyCommandHandler"/> class.
    /// </summary>
    public UpdateJourneyCommandHandler(IJourneyRepository journeyRepository, IUnitOfWork unitOfWork)
    {
        _journeyRepository = journeyRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(UpdateJourneyCommand request, CancellationToken cancellationToken)
    {
        var journey = await _journeyRepository.GetByIdAsync(request.Id, cancellationToken);

        if (journey is null)
        {
            return Result.Failure(new Error("Journey.NotFound", "Journey not found"));
        }

        if (journey.UserId != request.UserId)
        {
            return Result.Failure(new Error("Journey.Forbidden", "You are not authorized to update this journey"));
        }

        if (!Enum.TryParse<TransportType>(request.TransportType, true, out var transportType))
        {
            return Result.Failure(new Error(
                "Journey.InvalidTransportType",
                $"Invalid transport type: {request.TransportType}"));
        }

        var updateResult = journey.Update(
            request.StartLocation,
            request.StartTime,
            request.ArrivalLocation,
            request.ArrivalTime,
            transportType,
            request.DistanceKm);

        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        _journeyRepository.Update(journey);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
