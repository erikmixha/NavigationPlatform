using Journey.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Common.Result;

namespace Journey.Application.Queries.Admin.GetStatistics;

/// <summary>
/// Handler for getting monthly distance statistics with pagination.
/// </summary>
public sealed class GetMonthlyDistanceQueryHandler
    : IRequestHandler<GetMonthlyDistanceQuery, Result<List<MonthlyDistanceDto>>>
{
    private readonly IJourneyRepository _repository;
    private readonly ILogger<GetMonthlyDistanceQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetMonthlyDistanceQueryHandler"/> class.
    /// </summary>
    public GetMonthlyDistanceQueryHandler(
        IJourneyRepository repository,
        ILogger<GetMonthlyDistanceQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<List<MonthlyDistanceDto>>> Handle(
        GetMonthlyDistanceQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Page < 1 || request.PageSize < 1)
        {
            return Result.Failure<List<MonthlyDistanceDto>>(
                new Error("Validation.InvalidPagination", "Page and PageSize must be greater than 0"));
        }

        var readModels = await _repository.GetMonthlyDistanceReadModelsAsync(cancellationToken);

        var query = readModels
            .Select(m => new MonthlyDistanceDto
            {
                UserId = m.UserId,
                Year = m.Year,
                Month = m.Month,
                TotalDistanceKm = m.TotalDistanceKm
            })
            .AsQueryable();

        var isDescending = string.Equals(request.Direction, "desc", StringComparison.OrdinalIgnoreCase);
        query = (request.OrderBy?.ToLowerInvariant()) switch
        {
            "userid" => isDescending
                ? query.OrderByDescending(x => x.UserId).ThenByDescending(x => x.Year).ThenByDescending(x => x.Month)
                : query.OrderBy(x => x.UserId).ThenByDescending(x => x.Year).ThenByDescending(x => x.Month),
            "totaldistancekm" => isDescending
                ? query.OrderByDescending(x => x.TotalDistanceKm).ThenByDescending(x => x.Year).ThenByDescending(x => x.Month)
                : query.OrderBy(x => x.TotalDistanceKm).ThenByDescending(x => x.Year).ThenByDescending(x => x.Month),
            _ => query.OrderByDescending(x => x.Year).ThenByDescending(x => x.Month).ThenBy(x => x.UserId)
        };

        var monthlyData = query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return Result.Success(monthlyData);
    }
}
