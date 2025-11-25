using MediatR;
using Shared.Common.Result;

namespace Notification.Application.Commands.MarkAsUnread;

public sealed record MarkAsUnreadCommand : IRequest<Result>
{
    public Guid NotificationId { get; init; }
    public string UserId { get; init; } = string.Empty;
}

