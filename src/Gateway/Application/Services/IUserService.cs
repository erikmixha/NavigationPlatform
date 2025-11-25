using Gateway.Application.DTOs;

namespace Gateway.Application.Services;

/// <summary>
/// Service for user-related operations.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Gets the current authenticated user information.
    /// </summary>
    /// <param name="user">The claims principal representing the user.</param>
    /// <returns>User information DTO.</returns>
    UserInfoDto GetCurrentUser(System.Security.Claims.ClaimsPrincipal user);

    /// <summary>
    /// Gets a list of users for sharing purposes (excludes current user and admins).
    /// </summary>
    /// <param name="currentUserId">The ID of the current user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of user information DTOs.</returns>
    Task<IEnumerable<UserInfoDto>> GetUsersForSharingAsync(string currentUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users with their status (admin only).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of users with status DTOs.</returns>
    Task<IEnumerable<UserWithStatusDto>> GetUsersWithStatusAsync(CancellationToken cancellationToken = default);
}

