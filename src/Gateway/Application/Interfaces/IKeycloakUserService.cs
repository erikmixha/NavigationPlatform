using Gateway.Application.DTOs;

namespace Gateway.Application.Interfaces;

/// <summary>
/// Service for retrieving user information from Keycloak.
/// </summary>
public interface IKeycloakUserService
{
    /// <summary>
    /// Gets all users with "User" role, excluding the current user.
    /// </summary>
    /// <param name="currentUserId">The current user's ID to exclude from results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of user information.</returns>
    Task<IEnumerable<UserInfoDto>> GetUsersForSharingAsync(string currentUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users with their account status for admin operations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of users with status information.</returns>
    Task<IEnumerable<UserWithStatusDto>> GetUsersWithStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of a user from Keycloak.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user's status (Active, Suspended, or Unknown).</returns>
    Task<string> GetUserStatusAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a user's account status in Keycloak.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="enabled">True to enable (Active), false to disable (Suspended).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if update was successful, false otherwise.</returns>
    Task<bool> UpdateUserStatusAsync(string userId, bool enabled, CancellationToken cancellationToken = default);
}

