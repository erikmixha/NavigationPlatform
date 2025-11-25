using System.Diagnostics.CodeAnalysis;
using Gateway.Application.Commands.RefreshToken;
using Gateway.Application.DTOs;
using Gateway.Application.Queries.GetCurrentUser;
using Gateway.Application.Queries.GetUsersForSharing;
using Gateway.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MediatR;
using Shared.Common.Extensions;

namespace Gateway.Controllers;

/// <summary>
/// Controller for handling authentication operations.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Thin API controller wrapper around MediatR handlers and OIDC authentication.
/// Authentication flow is tested via integration tests.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "API controller - thin wrapper around MediatR and OIDC. Tested via integration tests.")]
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly IMediator _mediator;
    private readonly FrontendOptions _frontendOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    public AuthController(
        ILogger<AuthController> logger,
        IMediator mediator,
        IOptions<FrontendOptions> frontendOptions)
    {
        _logger = logger;
        _mediator = mediator;
        _frontendOptions = frontendOptions.Value;
    }

    /// <summary>
    /// Initiates the login flow by redirecting to Keycloak for OIDC authentication with PKCE.
    /// </summary>
    /// <param name="returnUrl">Optional URL to redirect to after successful login.</param>
    /// <returns>Challenge result that redirects to Keycloak.</returns>
    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login([FromQuery] string? returnUrl = null)
    {
        var redirectUrl = string.IsNullOrEmpty(returnUrl) ? _frontendOptions.Url : returnUrl;
        
        if (!string.IsNullOrEmpty(returnUrl) && !returnUrl.StartsWith("http"))
        {
            redirectUrl = $"{_frontendOptions.Url}{returnUrl}";
        }
        
        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectUrl,
            IsPersistent = true
        };

        return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Logs out the current user by revoking tokens and clearing authentication cookies.
    /// </summary>
    /// <returns>No content response.</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        var correlationId = HttpContext.TraceIdentifier;
        _logger.LogInformation("User logout initiated. CorrelationId: {CorrelationId}", correlationId);

        try
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);

            Response.Cookies.Delete(".NavPlat.Auth");
            Response.Cookies.Delete(".AspNetCore.OpenIdConnect.Nonce");
            Response.Cookies.Delete(".AspNetCore.Correlation");

            _logger.LogInformation("User logged out successfully. CorrelationId: {CorrelationId}", correlationId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout. CorrelationId: {CorrelationId}", correlationId);
            
            try
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
            }
            catch
            {
            }
            
            Response.Cookies.Delete(".NavPlat.Auth");
            Response.Cookies.Delete(".AspNetCore.OpenIdConnect.Nonce");
            Response.Cookies.Delete(".AspNetCore.Correlation");

            return NoContent();
        }
    }

    /// <summary>
    /// Gets information about the currently authenticated user.
    /// </summary>
    /// <returns>The current user information.</returns>
    [HttpGet("user")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var query = new GetCurrentUserQuery { User = User };
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblemDetails(HttpContext);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets a list of all users (excluding the current user) for sharing journeys.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of user information.</returns>
    [HttpGet("users")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<UserInfoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = User.FindFirst("sub")?.Value
                ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            
            if (string.IsNullOrEmpty(currentUserId))
            {
                return StatusCode(401, new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "Unauthorized",
                    status = 401,
                    detail = "User ID not found in claims",
                    traceId = HttpContext.TraceIdentifier
                });
            }

            var query = new GetUsersForSharingQuery { CurrentUserId = currentUserId };
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
            _logger.LogError(ex, "Error retrieving users from Keycloak");
            return StatusCode(500, new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                title = "Internal Server Error",
                status = 500,
                detail = "An error occurred while retrieving users.",
                traceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Refreshes the access token using the refresh token stored in the authentication cookie.
    /// </summary>
    /// <returns>No content if successful, unauthorized if refresh failed.</returns>
    [HttpGet("refresh")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken()
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!authenticateResult.Succeeded)
        {
            return Unauthorized(new
            {
                type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                title = "Unauthorized",
                status = 401,
                detail = "Authentication required",
                traceId = HttpContext.TraceIdentifier
            });
        }

        var refreshToken = authenticateResult.Properties?.GetTokenValue("refresh_token");
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new
            {
                type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                title = "Unauthorized",
                status = 401,
                detail = "No refresh token available",
                traceId = HttpContext.TraceIdentifier
            });
        }

        var command = new RefreshTokenCommand { RefreshToken = refreshToken };
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return Unauthorized(new
            {
                type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                title = "Unauthorized",
                status = 401,
                detail = result.Error.Message,
                traceId = HttpContext.TraceIdentifier
            });
        }

        var authProperties = authenticateResult.Properties ?? new AuthenticationProperties();
        authProperties.StoreTokens(new[]
        {
            new AuthenticationToken { Name = "access_token", Value = result.Value.AccessToken },
            new AuthenticationToken { Name = "refresh_token", Value = result.Value.RefreshToken },
            new AuthenticationToken { Name = "expires_at", Value = DateTime.UtcNow.AddSeconds(result.Value.ExpiresIn).ToString("o") }
        });

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, authenticateResult.Principal!, authProperties);

        _logger.LogInformation("Token refreshed successfully for user");
        return NoContent();
    }
}
