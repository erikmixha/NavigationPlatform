using Journey.Application.Interfaces;
using Journey.Application.DTOs;
using MediatR;
using Shared.Common.Result;

namespace Journey.Application.Queries.GetJourneysPaged;

/// <summary>
/// Handler for getting paginated journeys for a user.
/// </summary>
public sealed class GetJourneysPagedQueryHandler : IRequestHandler<GetJourneysPagedQuery, Result<PagedResult<JourneyDto>>>
{
    private readonly IJourneyRepository _journeyRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetJourneysPagedQueryHandler"/> class.
    /// </summary>
    public GetJourneysPagedQueryHandler(IJourneyRepository journeyRepository)
    {
        _journeyRepository = journeyRepository;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<JourneyDto>>> Handle(GetJourneysPagedQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _journeyRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.UserId,
            request.StartDateFrom,
            request.StartDateTo,
            cancellationToken);

        var favoriteIds = await _journeyRepository.GetFavoriteJourneyIdsAsync(request.UserId, cancellationToken);
        var favoriteSet = favoriteIds.ToHashSet();

        var dtos = items.Select(j => new JourneyDto
        {
            Id = j.Id,
            UserId = j.UserId,
            StartLocation = j.StartLocation,
            StartTime = j.StartTime,
            ArrivalLocation = j.ArrivalLocation,
            ArrivalTime = j.ArrivalTime,
            TransportType = j.TransportType.ToString(),
            DistanceKm = j.DistanceKm.Value,
            IsFavorite = favoriteSet.Contains(j.Id),
            IsShared = j.UserId != request.UserId
        }).ToList();

        var pagedResult = new PagedResult<JourneyDto>
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };

        return Result.Success(pagedResult);
    }
}
