using System.Diagnostics.CodeAnalysis;
using Shared.Common.Extensions;
using Shared.Common.Models;
using Journey.Application.DTOs;
using Journey.Application.Queries.Admin.GetAllJourneys;
using Journey.Application.Queries.Admin.GetStatistics;
using Journey.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Journey.API.Controllers;

/// <summary>
/// Controller for admin operations on journeys.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Thin API controller wrapper around MediatR handlers.
/// Business logic is tested in integration tests and handler unit tests.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "API controller - thin wrapper around MediatR. Tested via integration tests.")]
[ApiController]
[Route("admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminController"/> class.
    /// </summary>
    /// <param name="mediator">The MediatR mediator instance.</param>
    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets all journeys with pagination and filtering for admin operations.
    /// </summary>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 20).</param>
    /// <param name="userId">Optional user ID filter.</param>
    /// <param name="transportType">Optional transport type filter.</param>
    /// <param name="startDateFrom">Optional start date filter (from).</param>
    /// <param name="startDateTo">Optional start date filter (to).</param>
    /// <param name="arrivalDateFrom">Optional arrival date filter (from).</param>
    /// <param name="arrivalDateTo">Optional arrival date filter (to).</param>
    /// <param name="minDistance">Optional minimum distance filter.</param>
    /// <param name="maxDistance">Optional maximum distance filter.</param>
    /// <param name="orderBy">Optional field to order by.</param>
    /// <param name="direction">Optional sort direction (asc/desc).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of journeys with total count in X-Total-Count header.</returns>
    [HttpGet("journeys")]
    [ProducesResponseType(typeof(PagedResult<JourneyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllJourneys(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? userId = null,
        [FromQuery] TransportType? transportType = null,
        [FromQuery] DateTime? startDateFrom = null,
        [FromQuery] DateTime? startDateTo = null,
        [FromQuery] DateTime? arrivalDateFrom = null,
        [FromQuery] DateTime? arrivalDateTo = null,
        [FromQuery] decimal? minDistance = null,
        [FromQuery] decimal? maxDistance = null,
        [FromQuery] string? orderBy = null,
        [FromQuery] string? direction = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAllJourneysQuery(
            page,
            pageSize,
            userId,
            transportType,
            startDateFrom,
            startDateTo,
            arrivalDateFrom,
            arrivalDateTo,
            minDistance,
            maxDistance,
            orderBy,
            direction);

        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemDetails(HttpContext);
        }

        Response.Headers.Append("X-Total-Count", result.Value.TotalCount.ToString());

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets overall statistics for all journeys.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Statistics including total journeys, total distance, and average distance.</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(StatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStatistics(CancellationToken cancellationToken)
    {
        var query = new GetStatisticsQuery();
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemDetails(HttpContext);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets monthly distance statistics with pagination.
    /// </summary>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 20).</param>
    /// <param name="orderBy">Optional field to order by.</param>
    /// <param name="direction">Optional sort direction (asc/desc).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of monthly distance statistics.</returns>
    [HttpGet("statistics/monthly-distance")]
    [ProducesResponseType(typeof(List<MonthlyDistanceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMonthlyDistance(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? orderBy = null,
        [FromQuery] string? direction = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMonthlyDistanceQuery(page, pageSize, orderBy, direction);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemDetails(HttpContext);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets all journeys for a specific user with pagination.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 20).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of journeys for the user with total count in X-Total-Count header.</returns>
    [HttpGet("users/{userId}/journeys")]
    [ProducesResponseType(typeof(PagedResult<JourneyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserJourneys(
        string userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAllJourneysQuery(page, pageSize, userId);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemDetails(HttpContext);
        }

        Response.Headers.Append("X-Total-Count", result.Value.TotalCount.ToString());

        return Ok(result.Value);
    }
}
