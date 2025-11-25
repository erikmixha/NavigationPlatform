using MediatR;
using Shared.Common.Result;

namespace Journey.Application.Queries.GetSharedUsers;

public sealed record GetSharedUsersQuery : IRequest<Result<List<string>>>
{
    public Guid JourneyId { get; init; }
    public string UserId { get; init; } = string.Empty;
}

