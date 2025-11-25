namespace Gateway.Application.DTOs;

/// <summary>
/// Represents a token response from OIDC provider.
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

