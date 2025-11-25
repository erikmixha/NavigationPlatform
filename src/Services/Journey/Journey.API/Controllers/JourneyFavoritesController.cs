using System.Diagnostics.CodeAnalysis;
using Shared.Common.Models;
using Shared.Common.Extensions;
using Journey.Application.Commands.AddFavorite;
using Journey.Application.Commands.RemoveFavorite;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Journey.API.Controllers;

/// <summary>
/// Controller for managing journey favorites.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Thin API controller wrapper around MediatR handlers.
/// Business logic is tested in integration tests and handler unit tests.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "API controller - thin wrapper around MediatR. Tested via integration tests.")]
[ApiController]
[Route("api/journeys")]
[Authorize]
public class JourneyFavoritesController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="JourneyFavoritesController"/> class.
    /// </summary>
    /// <param name="mediator">The MediatR mediator instance.</param>
    public JourneyFavoritesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Adds a journey to favorites.
    /// </summary>
    /// <param name="id">The journey identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>OK if already favorited, NoContent if successfully added.</returns>
    [HttpPost("{id:guid}/favorite")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddFavorite(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new AddFavoriteCommand(id, GetUserId());
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "Journey.AlreadyFavorited")
            {
                return Ok(new { message = "Journey is already in favorites" });
            }
            return result.ToProblemDetails(HttpContext);
        }

        return NoContent();
    }

    /// <summary>
    /// Removes a journey from favorites.
    /// </summary>
    /// <param name="id">The journey identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{id:guid}/favorite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFavorite(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new RemoveFavoriteCommand(id, GetUserId());
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemDetails(HttpContext);
        }

        return NoContent();
    }

    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
        User.FindFirst("sub")?.Value ??
        throw new UnauthorizedAccessException("User ID not found in claims");
}
