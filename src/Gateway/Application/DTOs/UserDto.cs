namespace Gateway.Application.DTOs;

/// <summary>
/// Represents user information returned to the client.
/// </summary>
public sealed record UserDto
{
    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Gets or sets the user's display name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets or sets the user's roles.
    /// </summary>
    public List<string> Roles { get; init; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the user is authenticated.
    /// </summary>
    public bool IsAuthenticated { get; init; }
}

