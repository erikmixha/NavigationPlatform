using Journey.Application.DTOs;
using Journey.Domain.Enums;
using MediatR;
using Shared.Common.Result;

namespace Journey.Application.Queries.Admin.GetAllJourneys;

public sealed record GetAllJourneysQuery(
    int Page,
    int PageSize,
    string? UserId = null,
    TransportType? TransportType = null,
    DateTime? StartDateFrom = null,
    DateTime? StartDateTo = null,
    DateTime? ArrivalDateFrom = null,
    DateTime? ArrivalDateTo = null,
    decimal? MinDistance = null,
    decimal? MaxDistance = null,
    string? OrderBy = null,
    string? Direction = null) : IRequest<Result<PagedResult<JourneyDto>>>;

