using Journey.Application.Interfaces;
using Journey.Domain.Enums;
using MediatR;
using Shared.Common.Result;

namespace Journey.Application.Commands.CreateJourney;

/// <summary>
/// Handler for creating a new journey.
/// </summary>
public sealed class CreateJourneyCommandHandler : IRequestHandler<CreateJourneyCommand, Result<Guid>>
{
    private readonly IJourneyRepository _journeyRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateJourneyCommandHandler"/> class.
    /// </summary>
    public CreateJourneyCommandHandler(IJourneyRepository journeyRepository, IUnitOfWork unitOfWork)
    {
        _journeyRepository = journeyRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateJourneyCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TransportType>(request.TransportType, true, out var transportType))
        {
            return Result.Failure<Guid>(new Error(
                "Journey.InvalidTransportType",
                $"Invalid transport type: {request.TransportType}"));
        }

        var journeyResult = Domain.Entities.Journey.Create(
            request.UserId,
            request.StartLocation,
            request.StartTime,
            request.ArrivalLocation,
            request.ArrivalTime,
            transportType,
            request.DistanceKm);

        if (journeyResult.IsFailure)
        {
            return Result.Failure<Guid>(journeyResult.Error);
        }

        await _journeyRepository.AddAsync(journeyResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(journeyResult.Value.Id);
    }
}
