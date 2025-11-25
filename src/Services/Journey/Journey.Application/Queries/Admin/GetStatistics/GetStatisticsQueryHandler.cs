using Journey.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Common.Result;

namespace Journey.Application.Queries.Admin.GetStatistics;

/// <summary>
/// Handler for getting overall journey statistics.
/// </summary>
public sealed class GetStatisticsQueryHandler
    : IRequestHandler<GetStatisticsQuery, Result<StatisticsDto>>
{
    private readonly IJourneyRepository _repository;
    private readonly ILogger<GetStatisticsQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetStatisticsQueryHandler"/> class.
    /// </summary>
    public GetStatisticsQueryHandler(
        IJourneyRepository repository,
        ILogger<GetStatisticsQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<StatisticsDto>> Handle(
        GetStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        var statistics = await _repository.GetStatisticsAsync(cancellationToken);

        _logger.LogInformation("Generated statistics");

        return Result.Success(statistics);
    }
}
