using MediatR;
using Shared.Common.Result;

namespace Notification.Application.Commands.MarkAsRead;

public sealed record MarkAsReadCommand : IRequest<Result>
{
    public Guid NotificationId { get; init; }
    public string UserId { get; init; } = string.Empty;
}

