using System.Diagnostics.CodeAnalysis;

namespace Gateway.Domain.Entities;

/// <remarks>
/// Excluded from code coverage: Simple domain entity with minimal business logic.
/// Tested via integration tests.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Simple domain entity. Tested via integration tests.")]
public sealed class User
{
    private User()
    {
    }

    public User(string keycloakId, string username, string? email, string? firstName, string? lastName, string? roles = null)
    {
        KeycloakId = keycloakId;
        Username = username;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        Roles = roles ?? string.Empty;
        LastLoginAt = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
    }

    public string KeycloakId { get; private set; } = string.Empty;
    public string Username { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string Roles { get; private set; } = string.Empty;
    public DateTime LastLoginAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string? email, string? firstName, string? lastName, string? roles = null)
    {
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        if (roles != null)
        {
            Roles = roles;
        }
    }

    public bool HasRole(string role)
    {
        if (string.IsNullOrEmpty(Roles))
            return false;
        
        var roleList = Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return roleList.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}
