using Journey.Application.Interfaces;
using Journey.Application.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Common.Result;

namespace Journey.Application.Queries.Admin.GetAllJourneys;

/// <summary>
/// Handler for getting all journeys with pagination and filtering for admin operations.
/// </summary>
public sealed class GetAllJourneysQueryHandler
    : IRequestHandler<GetAllJourneysQuery, Result<PagedResult<JourneyDto>>>
{
    private readonly IJourneyRepository _repository;
    private readonly ILogger<GetAllJourneysQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAllJourneysQueryHandler"/> class.
    /// </summary>
    public GetAllJourneysQueryHandler(
        IJourneyRepository repository,
        ILogger<GetAllJourneysQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<JourneyDto>>> Handle(
        GetAllJourneysQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Page < 1 || request.PageSize < 1)
        {
            return Result.Failure<PagedResult<JourneyDto>>(
                new Error("Validation.InvalidPagination", "Page and PageSize must be greater than 0"));
        }

        var totalCount = await _repository.GetTotalCountAsync(
            request.UserId,
            request.TransportType,
            request.StartDateFrom,
            request.StartDateTo,
            request.ArrivalDateFrom,
            request.ArrivalDateTo,
            request.MinDistance,
            request.MaxDistance,
            cancellationToken);

        var journeys = await _repository.GetAllPagedAsync(
            request.Page,
            request.PageSize,
            request.UserId,
            request.TransportType,
            request.StartDateFrom,
            request.StartDateTo,
            request.ArrivalDateFrom,
            request.ArrivalDateTo,
            request.MinDistance,
            request.MaxDistance,
            request.OrderBy,
            request.Direction,
            cancellationToken);

        var journeyDtos = journeys.Select(j => new JourneyDto
        {
            Id = j.Id,
            UserId = j.UserId,
            StartLocation = j.StartLocation,
            StartTime = j.StartTime,
            ArrivalLocation = j.ArrivalLocation,
            ArrivalTime = j.ArrivalTime,
            TransportType = j.TransportType.ToString(),
            DistanceKm = j.DistanceKm.Value,
            IsFavorite = false,
            IsShared = false
        }).ToList();

        var pagedResult = new PagedResult<JourneyDto>
        {
            Items = journeyDtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };

        return Result.Success(pagedResult);
    }
}
