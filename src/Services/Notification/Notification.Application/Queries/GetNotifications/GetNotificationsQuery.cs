using MediatR;
using Notification.Application.DTOs;
using Shared.Common.Result;

namespace Notification.Application.Queries.GetNotifications;

public sealed record GetNotificationsQuery : IRequest<Result<List<NotificationDto>>>
{
    public string UserId { get; init; } = string.Empty;
    public bool? IsRead { get; init; }
}

