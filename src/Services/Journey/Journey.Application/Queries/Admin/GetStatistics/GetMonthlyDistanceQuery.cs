using MediatR;
using Shared.Common.Result;

namespace Journey.Application.Queries.Admin.GetStatistics;

public sealed record GetMonthlyDistanceQuery(
    int Page = 1,
    int PageSize = 20,
    string? OrderBy = null,
    string? Direction = null) : IRequest<Result<List<MonthlyDistanceDto>>>;

