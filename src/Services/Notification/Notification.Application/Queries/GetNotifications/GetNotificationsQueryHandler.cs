using MediatR;
using Notification.Application.Interfaces;
using Notification.Application.DTOs;
using Shared.Common.Result;

namespace Notification.Application.Queries.GetNotifications;

/// <summary>
/// Handler for getting notifications for a user.
/// </summary>
public sealed class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, Result<List<NotificationDto>>>
{
    private readonly INotificationRepository _notificationRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetNotificationsQueryHandler"/> class.
    /// </summary>
    public GetNotificationsQueryHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    /// <inheritdoc />
    public async Task<Result<List<NotificationDto>>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var notifications = await _notificationRepository.GetByUserIdAsync(request.UserId, request.IsRead, cancellationToken);

        var dtos = notifications.Select(n => new NotificationDto
        {
            Id = n.Id,
            Type = n.Type,
            Title = n.Title,
            Message = n.Message,
            IsRead = n.IsRead,
            CreatedOnUtc = n.CreatedOnUtc,
            ReadOnUtc = n.ReadOnUtc
        }).ToList();

        return Result.Success(dtos);
    }
}
