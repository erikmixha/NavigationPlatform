namespace Gateway.Application.DTOs;

/// <summary>
/// Data transfer object for user information used in sharing operations.
/// </summary>
public sealed record UserInfoDto
{
    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's display name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string Username { get; init; } = string.Empty;
}

