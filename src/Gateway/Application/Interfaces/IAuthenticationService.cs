using Gateway.Application.DTOs;

namespace Gateway.Application.Interfaces;

/// <summary>
/// Service for handling authentication operations.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Refreshes the access token using the refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new token response, or null if refresh failed.</returns>
    Task<TokenResponse?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}

