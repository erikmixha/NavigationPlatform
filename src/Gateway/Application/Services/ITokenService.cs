namespace Gateway.Application.Services;

/// <summary>
/// Service for token management operations.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Refreshes an access token using a refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Token response containing new access and refresh tokens, or null if refresh failed.</returns>
    Task<TokenResponse?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}

/// <summary>
/// Response model for token refresh operations.
/// </summary>
public sealed record TokenResponse
{
    /// <summary>
    /// Gets or sets the access token.
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the refresh token.
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the expiration time in seconds.
    /// </summary>
    public int ExpiresIn { get; init; }
}

