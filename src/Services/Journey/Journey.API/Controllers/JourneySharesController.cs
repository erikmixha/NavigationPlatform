using System.Diagnostics.CodeAnalysis;
using Shared.Common.Models;
using Shared.Common.Extensions;
using Journey.Application.Commands.GeneratePublicLink;
using Journey.Application.Commands.RevokePublicLink;
using Journey.Application.Commands.ShareJourney;
using Journey.Application.Commands.UnshareJourney;
using Journey.Application.Queries.GetJourneyByPublicLink;
using Journey.Application.Queries.GetSharedUsers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace Journey.API.Controllers;

/// <summary>
/// Controller for managing journey sharing and public links.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Thin API controller wrapper around MediatR handlers.
/// Business logic is tested in integration tests and handler unit tests.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "API controller - thin wrapper around MediatR. Tested via integration tests.")]
[ApiController]
[Route("api/journeys")]
[Authorize]
public class JourneySharesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="JourneySharesController"/> class.
    /// </summary>
    /// <param name="mediator">The MediatR mediator instance.</param>
    /// <param name="configuration">The configuration instance.</param>
    public JourneySharesController(IMediator mediator, IConfiguration configuration)
    {
        _mediator = mediator;
        _configuration = configuration;
    }

    /// <summary>
    /// Shares a journey with one or more users.
    /// </summary>
    /// <param name="id">The journey identifier.</param>
    /// <param name="request">The share request containing user IDs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpPost("{id:guid}/share")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ShareJourney(
        Guid id,
        [FromBody] ShareJourneyRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SharedWithUserIds == null || !request.SharedWithUserIds.Any())
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Bad Request",
                Status = 400,
                Detail = "At least one user ID is required",
                Instance = HttpContext.Request.Path,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var command = new ShareJourneyCommand(id, request.SharedWithUserIds, GetUserId());
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemDetails(HttpContext);
        }

        return NoContent();
    }

    /// <summary>
    /// Unshares a journey from a specific user.
    /// </summary>
    /// <param name="id">The journey identifier.</param>
    /// <param name="userId">The user ID to unshare from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{id:guid}/share/{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnshareJourney(
        Guid id,
        string userId,
        CancellationToken cancellationToken)
    {
        var command = new UnshareJourneyCommand(id, userId, GetUserId());
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemDetails(HttpContext);
        }

        return NoContent();
    }

    /// <summary>
    /// Generates a public link for a journey.
    /// </summary>
    /// <param name="id">The journey identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The public link response with token and URL.</returns>
    [HttpPost("{id:guid}/public-link")]
    [ProducesResponseType(typeof(PublicLinkResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GeneratePublicLink(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new GeneratePublicLinkCommand(id, GetUserId());
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemDetails(HttpContext);
        }

        var frontendUrl = _configuration["Frontend:Url"] ?? $"{Request.Scheme}://{Request.Host}";
        return Ok(new PublicLinkResponse
        {
            Token = result.Value,
            Url = $"{frontendUrl}/journeys/public/{result.Value}"
        });
    }

    /// <summary>
    /// Revokes the public link for a journey.
    /// </summary>
    /// <param name="id">The journey identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{id:guid}/public-link")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokePublicLink(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new RevokePublicLinkCommand(id, GetUserId());
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemDetails(HttpContext);
        }

        return NoContent();
    }

    /// <summary>
    /// Gets the list of users a journey is shared with.
    /// </summary>
    /// <param name="id">The journey identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of user IDs the journey is shared with.</returns>
    [HttpGet("{id:guid}/share")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSharedUsers(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetSharedUsersQuery { JourneyId = id, UserId = GetUserId() };
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemDetails(HttpContext);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets a journey by its public link token (anonymous access).
    /// </summary>
    /// <param name="token">The public link token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The journey details if the link is valid and active.</returns>
    [HttpGet("public/{token}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Application.DTOs.JourneyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status410Gone)]
    public async Task<IActionResult> GetJourneyByPublicLink(
        string token,
        CancellationToken cancellationToken)
    {
        var query = new GetJourneyByPublicLinkQuery(token);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemDetails(HttpContext);
        }

        return Ok(result.Value);
    }

    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
        User.FindFirst("sub")?.Value ??
        throw new UnauthorizedAccessException("User ID not found in claims");
}
