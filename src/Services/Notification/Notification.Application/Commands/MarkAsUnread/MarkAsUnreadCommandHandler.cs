using MediatR;
using Notification.Application.Interfaces;
using Shared.Common.Result;

namespace Notification.Application.Commands.MarkAsUnread;

/// <summary>
/// Handler for marking a notification as unread.
/// </summary>
public sealed class MarkAsUnreadCommandHandler : IRequestHandler<MarkAsUnreadCommand, Result>
{
    private readonly INotificationRepository _notificationRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkAsUnreadCommandHandler"/> class.
    /// </summary>
    public MarkAsUnreadCommandHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(MarkAsUnreadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);

        if (notification is null)
        {
            return Result.Failure(new Error("Notification.NotFound", "Notification not found"));
        }

        if (notification.UserId != request.UserId)
        {
            return Result.Failure(new Error("Notification.Forbidden", "You are not authorized to modify this notification"));
        }

        notification.MarkAsUnread();
        _notificationRepository.Update(notification);
        await _notificationRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
