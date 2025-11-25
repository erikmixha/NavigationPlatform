using System.Diagnostics.CodeAnalysis;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notification.API.Extensions;
using Notification.API.Models;
using Notification.Application.Commands.MarkAsRead;
using Notification.Application.Commands.MarkAsUnread;
using Notification.Application.Queries.GetNotifications;
using Notification.Application.Queries.GetUnreadCount;
using System.Security.Claims;

namespace Notification.API.Controllers;

/// <summary>
/// Controller for managing user notifications.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Thin API controller wrapper around MediatR handlers.
/// Business logic is tested in integration tests and handler unit tests.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "API controller - thin wrapper around MediatR. Tested via integration tests.")]
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationsController"/> class.
    /// </summary>
    /// <param name="mediator">The MediatR mediator instance.</param>
    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets notifications for the current user, optionally filtered by read status.
    /// </summary>
    /// <param name="isRead">Optional filter for read status (true for read, false for unread, null for all).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of notifications.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<Application.DTOs.NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] bool? isRead = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetNotificationsQuery
        {
            UserId = GetUserId(),
            IsRead = isRead
        };

        var result = await _mediator.Send(query, cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails(HttpContext)
            : Ok(result.Value);
    }

    /// <summary>
    /// Gets the count of unread notifications for the current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The unread notification count.</returns>
    [HttpGet("unread/count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken = default)
    {
        var query = new GetUnreadCountQuery
        {
            UserId = GetUserId()
        };

        var result = await _mediator.Send(query, cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails(HttpContext)
            : Ok(new { count = result.Value });
    }

    /// <summary>
    /// Marks a notification as read.
    /// </summary>
    /// <param name="id">The notification identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new MarkAsReadCommand
        {
            NotificationId = id,
            UserId = GetUserId()
        };

        var result = await _mediator.Send(command, cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails(HttpContext)
            : NoContent();
    }

    /// <summary>
    /// Marks a notification as unread.
    /// </summary>
    /// <param name="id">The notification identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpPost("{id:guid}/unread")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAsUnread(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new MarkAsUnreadCommand
        {
            NotificationId = id,
            UserId = GetUserId()
        };

        var result = await _mediator.Send(command, cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails(HttpContext)
            : NoContent();
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("User ID not found");
    }
}
