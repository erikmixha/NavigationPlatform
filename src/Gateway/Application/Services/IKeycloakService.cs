namespace Gateway.Application.Services;

/// <summary>
/// Service for Keycloak administration operations.
/// </summary>
public interface IKeycloakService
{
    /// <summary>
    /// Gets the status of a user from Keycloak.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user status (Active, Suspended, or Unknown).</returns>
    Task<string> GetUserStatusAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of a user in Keycloak.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="enabled">Whether the user should be enabled.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the update was successful, false otherwise.</returns>
    Task<bool> UpdateUserStatusAsync(string userId, bool enabled, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users from Keycloak with their roles.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of user information with roles.</returns>
    Task<IEnumerable<KeycloakUserInfo>> GetUsersWithRolesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Keycloak user information with roles.
/// </summary>
public sealed record KeycloakUserInfo
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the email.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Gets or sets the first name.
    /// </summary>
    public string? FirstName { get; init; }

    /// <summary>
    /// Gets or sets the last name.
    /// </summary>
    public string? LastName { get; init; }

    /// <summary>
    /// Gets or sets whether the user is enabled.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Gets or sets the user's roles.
    /// </summary>
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
}

