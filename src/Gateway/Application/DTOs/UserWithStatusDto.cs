namespace Gateway.Application.DTOs;

/// <summary>
/// Data transfer object for user information with account status for admin operations.
/// </summary>
public sealed record UserWithStatusDto
{
    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's display name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the account status (Active or Suspended).
    /// </summary>
    public string Status { get; init; } = string.Empty;
}

