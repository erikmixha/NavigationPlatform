using MediatR;
using Notification.Application.Interfaces;
using Shared.Common.Result;

namespace Notification.Application.Commands.MarkAsRead;

/// <summary>
/// Handler for marking a notification as read.
/// </summary>
public sealed class MarkAsReadCommandHandler : IRequestHandler<MarkAsReadCommand, Result>
{
    private readonly INotificationRepository _notificationRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkAsReadCommandHandler"/> class.
    /// </summary>
    public MarkAsReadCommandHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(MarkAsReadCommand request, CancellationToken cancellationToken)
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

        notification.MarkAsRead();
        _notificationRepository.Update(notification);
        await _notificationRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
