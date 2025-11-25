using System.Diagnostics.CodeAnalysis;
using Journey.API.DTOs;
using Journey.API.Extensions;
using Shared.Common.Extensions;
using Shared.Common.Models;
using Journey.Application.Queries.GetJourneyById;
using Journey.Application.Queries.GetJourneysPaged;
using MediatR;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Journey.API.Controllers;

/// <summary>
/// Controller for managing journeys.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Thin API controller wrapper around MediatR handlers.
/// Business logic is tested in integration tests and handler unit tests.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "API controller - thin wrapper around MediatR. Tested via integration tests.")]
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JourneysController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="JourneysController"/> class.
    /// </summary>
    /// <param name="mediator">The MediatR mediator instance.</param>
    public JourneysController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new journey.
    /// </summary>
    /// <param name="request">The journey creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created journey response with ID.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreateJourneyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateJourney(
        [FromBody] CreateJourneyRequest request,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(GetUserId());
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemDetails(HttpContext);
        }

        return CreatedAtAction(
            nameof(GetJourney),
            new { id = result.Value },
            new CreateJourneyResponse { Id = result.Value });
    }

    /// <summary>
    /// Gets a journey by its identifier.
    /// </summary>
    /// <param name="id">The journey identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The journey details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Application.DTOs.JourneyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetJourney(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var isAdmin = User.IsInRole("Admin");
        var query = new GetJourneyByIdQuery { Id = id, UserId = userId, IsAdmin = isAdmin };
        var result = await _mediator.Send(query, cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails(HttpContext)
            : Ok(result.Value);
    }

    /// <summary>
    /// Gets paginated journeys for the current user with optional date filtering.
    /// </summary>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 20, max: 100).</param>
    /// <param name="startDateFrom">Optional start date filter (from).</param>
    /// <param name="startDateTo">Optional start date filter (to).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of journeys.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(Application.DTOs.PagedResult<Application.DTOs.JourneyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetJourneys(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? startDateFrom = null,
        [FromQuery] DateTime? startDateTo = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetJourneysPagedQuery
        {
            UserId = GetUserId(),
            Page = page,
            PageSize = Math.Min(pageSize, 100),
            StartDateFrom = startDateFrom,
            StartDateTo = startDateTo
        };

        var result = await _mediator.Send(query, cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails(HttpContext)
            : Ok(result.Value);
    }

    /// <summary>
    /// Updates an existing journey.
    /// </summary>
    /// <param name="id">The journey identifier.</param>
    /// <param name="request">The journey update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateJourney(
        Guid id,
        [FromBody] UpdateJourneyRequest request,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(id, GetUserId());
        var result = await _mediator.Send(command, cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails(HttpContext)
            : NoContent();
    }

    /// <summary>
    /// Deletes a journey.
    /// </summary>
    /// <param name="id">The journey identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteJourney(Guid id, CancellationToken cancellationToken)
    {
        var command = MappingExtensions.ToCommand(id, GetUserId());
        var result = await _mediator.Send(command, cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails(HttpContext)
            : NoContent();
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("User ID not found in claims");
    }
}
