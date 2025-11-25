using MediatR;
using Shared.Common.Result;

namespace Notification.Application.Queries.GetUnreadCount;

public sealed record GetUnreadCountQuery : IRequest<Result<int>>
{
    public string UserId { get; init; } = string.Empty;
}

