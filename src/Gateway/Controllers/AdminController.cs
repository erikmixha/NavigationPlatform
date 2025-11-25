using System.Diagnostics.CodeAnalysis;
using Gateway.Application.Commands.UpdateUserStatus;
using Gateway.Application.DTOs;
using Gateway.Application.Queries.GetUsersWithStatus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Shared.Common.Extensions;

namespace Gateway.Controllers;

/// <summary>
/// Controller for admin operations requiring Admin role.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Thin API controller wrapper around MediatR handlers.
/// Business logic is tested in integration tests and handler unit tests.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "API controller - thin wrapper around MediatR. Tested via integration tests.")]
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly ILogger<AdminController> _logger;
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminController"/> class.
    /// </summary>
    public AdminController(
        ILogger<AdminController> logger,
        IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    /// <summary>
    /// Gets all users with their account status for admin management.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of users with status information.</returns>
    [HttpGet("users")]
    [ProducesResponseType(typeof(IEnumerable<UserWithStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsersWithStatus(CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetUsersWithStatusQuery();
            var result = await _mediator.Send(query, cancellationToken);

            if (result.IsFailure)
            {
                return StatusCode(500, new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    title = "Internal Server Error",
                    status = 500,
                    detail = result.Error.Message,
                    traceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users with status");
            return StatusCode(500, new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                title = "Internal Server Error",
                status = 500,
                detail = "An error occurred while retrieving users",
                traceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Updates a user's account status (Active or Suspended).
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="request">The status update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpPatch("users/{userId}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserStatus(
        string userId,
        [FromBody] UpdateUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = "Bad Request",
                status = 400,
                detail = "User ID is required",
                traceId = HttpContext.TraceIdentifier
            });
        }

        try
        {
            var changedByUserId = User.FindFirst("sub")?.Value ?? "unknown";
            var command = new UpdateUserStatusCommand
            {
                UserId = userId,
                NewStatus = request.Status,
                ChangedByUserId = changedByUserId
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                if (result.Error.Code == "Status.Invalid")
                {
                    return BadRequest(new
                    {
                        type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                        title = "Bad Request",
                        status = 400,
                        detail = result.Error.Message,
                        traceId = HttpContext.TraceIdentifier
                    });
                }

                if (result.Error.Code == "User.NotFound")
                {
                    return NotFound(new
                    {
                        type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                        title = "Not Found",
                        status = 404,
                        detail = result.Error.Message,
                        traceId = HttpContext.TraceIdentifier
                    });
                }

                return StatusCode(500, new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    title = "Internal Server Error",
                    status = 500,
                    detail = result.Error.Message,
                    traceId = HttpContext.TraceIdentifier
                });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user status for {UserId}", userId);
            return StatusCode(500, new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                title = "Internal Server Error",
                status = 500,
                detail = "An error occurred while updating user status",
                traceId = HttpContext.TraceIdentifier
            });
        }
    }
}
