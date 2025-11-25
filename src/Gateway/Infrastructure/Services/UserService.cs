using Gateway.Application.DTOs;
using Gateway.Application.Services;
using Microsoft.Extensions.Logging;

namespace Gateway.Infrastructure.Services;

/// <summary>
/// Service for user-related operations.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Infrastructure service for user operations.
/// User operations are tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Infrastructure service - user operations tested via integration tests.")]
public sealed class UserService : IUserService
{
    private readonly IKeycloakService _keycloakService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IKeycloakService keycloakService,
        ILogger<UserService> logger)
    {
        _keycloakService = keycloakService;
        _logger = logger;
    }

    /// <inheritdoc />
    public UserInfoDto GetCurrentUser(System.Security.Claims.ClaimsPrincipal user)
    {
        var userId = user.FindFirst("sub")?.Value
            ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        var email = user.FindFirst("email")?.Value;
        var name = user.FindFirst("name")?.Value;
        var username = user.FindFirst("preferred_username")?.Value
            ?? user.FindFirst("name")?.Value
            ?? email
            ?? userId
            ?? string.Empty;
        var roles = user.FindAll("role").Select(c => c.Value).ToList();

        return new UserInfoDto
        {
            UserId = userId ?? string.Empty,
            Email = email ?? string.Empty,
            Name = name ?? string.Empty,
            Username = username
        };
    }

    /// <inheritdoc />
    public async Task<IEnumerable<UserInfoDto>> GetUsersForSharingAsync(string currentUserId, CancellationToken cancellationToken = default)
    {
        var users = await _keycloakService.GetUsersWithRolesAsync(cancellationToken);

        var result = users
            .Where(u => u.UserId != currentUserId
                && u.Roles.Contains("User")
                && !u.Roles.Contains("Admin"))
            .Select(u => new UserInfoDto
            {
                UserId = u.UserId,
                Email = u.Email ?? string.Empty,
                Name = GetDisplayName(u.FirstName, u.LastName, u.Username),
                Username = u.Username
            })
            .OrderBy(u => u.Name)
            .ToList();

        _logger.LogInformation("Retrieved {Count} users from Keycloak for user {UserId}", result.Count, currentUserId);

        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<UserWithStatusDto>> GetUsersWithStatusAsync(CancellationToken cancellationToken = default)
    {
        var users = await _keycloakService.GetUsersWithRolesAsync(cancellationToken);

        var result = users
            .Where(u => u.Roles.Contains("User") && !u.Roles.Contains("Admin"))
            .Select(u => new UserWithStatusDto
            {
                UserId = u.UserId,
                Username = u.Username,
                Email = u.Email ?? string.Empty,
                Name = GetDisplayName(u.FirstName, u.LastName, u.Username),
                Status = u.Enabled ? "Active" : "Suspended"
            })
            .OrderBy(u => u.Name)
            .ToList();

        return result;
    }

    private static string GetDisplayName(string? firstName, string? lastName, string username)
    {
        if (string.IsNullOrEmpty(firstName) && string.IsNullOrEmpty(lastName))
        {
            return username;
        }

        return $"{firstName} {lastName}".Trim();
    }
}

