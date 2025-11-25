using Journey.Application.Interfaces;
using Journey.Application.DTOs;
using MediatR;
using Shared.Common.Result;

namespace Journey.Application.Queries.GetJourneyById;

/// <summary>
/// Handler for getting a journey by its identifier.
/// </summary>
public sealed class GetJourneyByIdQueryHandler : IRequestHandler<GetJourneyByIdQuery, Result<JourneyDto>>
{
    private readonly IJourneyRepository _journeyRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetJourneyByIdQueryHandler"/> class.
    /// </summary>
    public GetJourneyByIdQueryHandler(IJourneyRepository journeyRepository)
    {
        _journeyRepository = journeyRepository;
    }

    /// <inheritdoc />
    public async Task<Result<JourneyDto>> Handle(GetJourneyByIdQuery request, CancellationToken cancellationToken)
    {
        var journey = await _journeyRepository.GetByIdAsync(request.Id, cancellationToken);

        if (journey is null)
        {
            return Result.Failure<JourneyDto>(new Error("Journey.NotFound", "Journey not found"));
        }

        if (!request.IsAdmin)
        {
            var canAccess = await _journeyRepository.CanAccessJourneyAsync(journey.Id, request.UserId, cancellationToken);
            if (!canAccess)
            {
                return Result.Failure<JourneyDto>(new Error("Journey.Forbidden", "You are not authorized to view this journey"));
            }
        }

        var isFavorite = await _journeyRepository.GetFavoriteAsync(journey.Id, request.UserId, cancellationToken) is not null;
        var isShared = journey.UserId != request.UserId;

        var dto = new JourneyDto
        {
            Id = journey.Id,
            UserId = journey.UserId,
            StartLocation = journey.StartLocation,
            StartTime = journey.StartTime,
            ArrivalLocation = journey.ArrivalLocation,
            ArrivalTime = journey.ArrivalTime,
            TransportType = journey.TransportType.ToString(),
            DistanceKm = journey.DistanceKm.Value,
            IsFavorite = isFavorite,
            IsShared = isShared
        };

        return Result.Success(dto);
    }
}
