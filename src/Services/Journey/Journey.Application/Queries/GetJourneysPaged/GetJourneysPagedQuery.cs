using Journey.Application.DTOs;
using MediatR;
using Shared.Common.Result;

namespace Journey.Application.Queries.GetJourneysPaged;

public sealed record GetJourneysPagedQuery : IRequest<Result<PagedResult<JourneyDto>>>
{
    public string UserId { get; init; } = string.Empty;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public DateTime? StartDateFrom { get; init; }
    public DateTime? StartDateTo { get; init; }
}

