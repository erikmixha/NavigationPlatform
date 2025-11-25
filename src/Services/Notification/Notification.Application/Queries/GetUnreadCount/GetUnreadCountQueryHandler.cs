using MediatR;
using Notification.Application.Interfaces;
using Shared.Common.Result;

namespace Notification.Application.Queries.GetUnreadCount;

/// <summary>
/// Handler for getting the unread notification count for a user.
/// </summary>
public sealed class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, Result<int>>
{
    private readonly INotificationRepository _notificationRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetUnreadCountQueryHandler"/> class.
    /// </summary>
    public GetUnreadCountQueryHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    /// <inheritdoc />
    public async Task<Result<int>> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        var count = await _notificationRepository.GetUnreadCountAsync(request.UserId, cancellationToken);
        return Result.Success(count);
    }
}
