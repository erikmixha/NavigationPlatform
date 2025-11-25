using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Gateway.Application.DTOs;
using Gateway.Application.Interfaces;
using Gateway.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Common.Configuration;

namespace Gateway.Application.Services;

/// <summary>
/// Service for retrieving and managing user information from Keycloak.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Application service for Keycloak user operations.
/// External service integration is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Application service - Keycloak integration tested via integration tests.")]
public sealed class KeycloakUserService : IKeycloakUserService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly AuthenticationOptions _authOptions;
    private readonly KeycloakOptions _keycloakOptions;
    private readonly KeycloakAdminService _keycloakAdminService;
    private readonly ILogger<KeycloakUserService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeycloakUserService"/> class.
    /// </summary>
    public KeycloakUserService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IOptions<AuthenticationOptions> authOptions,
        IOptions<KeycloakOptions> keycloakOptions,
        KeycloakAdminService keycloakAdminService,
        ILogger<KeycloakUserService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _authOptions = authOptions.Value;
        _keycloakOptions = keycloakOptions.Value;
        _keycloakAdminService = keycloakAdminService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<UserInfoDto>> GetUsersForSharingAsync(string currentUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(currentUserId))
            {
                throw new ArgumentException("Current user ID is required", nameof(currentUserId));
            }

            var adminToken = await _keycloakAdminService.GetAdminAccessTokenAsync(cancellationToken);
            var baseUrl = GetKeycloakBaseUrl();
            var realm = _authOptions.Realm;
            var usersEndpoint = $"{baseUrl}/admin/realms/{realm}/users";

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var response = await httpClient.GetAsync(usersEndpoint, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get users from Keycloak. Status: {StatusCode}", response.StatusCode);
                throw new InvalidOperationException("Failed to retrieve users from Keycloak");
            }

            var usersContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var users = JsonSerializer.Deserialize<JsonElement[]>(usersContent) ?? Array.Empty<JsonElement>();

            var result = new List<UserInfoDto>();
            foreach (var user in users)
            {
                var userId = user.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;

                if (string.IsNullOrEmpty(userId) || userId == currentUserId)
                {
                    continue;
                }

                var userRoles = await GetUserRolesAsync(baseUrl, realm, userId, adminToken, cancellationToken);

                if (userRoles.Contains("User") && !userRoles.Contains("Admin"))
                {
                    var username = user.TryGetProperty("username", out var usernameProp) ? usernameProp.GetString() : null;
                    var email = user.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;
                    var firstName = user.TryGetProperty("firstName", out var firstNameProp) ? firstNameProp.GetString() : null;
                    var lastName = user.TryGetProperty("lastName", out var lastNameProp) ? lastNameProp.GetString() : null;

                    result.Add(new UserInfoDto
                    {
                        UserId = userId,
                        Email = email ?? string.Empty,
                        Name = string.IsNullOrEmpty(firstName) && string.IsNullOrEmpty(lastName)
                            ? username ?? string.Empty
                            : $"{firstName} {lastName}".Trim(),
                        Username = username ?? string.Empty
                    });
                }
            }

            _logger.LogInformation("Retrieved {Count} users from Keycloak for user {UserId}", result.Count, currentUserId);
            return result.OrderBy(u => u.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users from Keycloak");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<UserWithStatusDto>> GetUsersWithStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var adminToken = await _keycloakAdminService.GetAdminAccessTokenAsync(cancellationToken);
            var baseUrl = GetKeycloakBaseUrl();
            var realm = _authOptions.Realm;
            var usersEndpoint = $"{baseUrl}/admin/realms/{realm}/users";

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var response = await httpClient.GetAsync(usersEndpoint, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get users from Keycloak. Status: {StatusCode}", response.StatusCode);
                throw new InvalidOperationException("Failed to retrieve users from Keycloak");
            }

            var usersContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var users = JsonSerializer.Deserialize<JsonElement[]>(usersContent) ?? Array.Empty<JsonElement>();

            var result = new List<UserWithStatusDto>();
            foreach (var user in users)
            {
                var userId = user.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                if (string.IsNullOrEmpty(userId))
                {
                    continue;
                }

                var userRoles = await GetUserRolesAsync(baseUrl, realm, userId, adminToken, cancellationToken);

                if (userRoles.Contains("User") && !userRoles.Contains("Admin"))
                {
                    var username = user.TryGetProperty("username", out var usernameProp) ? usernameProp.GetString() : null;
                    var email = user.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;
                    var firstName = user.TryGetProperty("firstName", out var firstNameProp) ? firstNameProp.GetString() : null;
                    var lastName = user.TryGetProperty("lastName", out var lastNameProp) ? lastNameProp.GetString() : null;
                    var enabled = user.TryGetProperty("enabled", out var enabledProp) && enabledProp.GetBoolean();

                    result.Add(new UserWithStatusDto
                    {
                        UserId = userId,
                        Username = username ?? string.Empty,
                        Email = email ?? string.Empty,
                        Name = string.IsNullOrEmpty(firstName) && string.IsNullOrEmpty(lastName)
                            ? username ?? string.Empty
                            : $"{firstName} {lastName}".Trim(),
                        Status = enabled ? "Active" : "Suspended"
                    });
                }
            }

            return result.OrderBy(u => u.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users with status");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<string> GetUserStatusAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminToken = await _keycloakAdminService.GetAdminAccessTokenAsync(cancellationToken);
            var baseUrl = GetKeycloakBaseUrl();
            var realm = _authOptions.Realm;
            var userEndpoint = $"{baseUrl}/admin/realms/{realm}/users/{userId}";

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var response = await httpClient.GetAsync(userEndpoint, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var userContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var user = JsonSerializer.Deserialize<JsonElement>(userContent);

                if (user.TryGetProperty("enabled", out var enabled))
                {
                    return enabled.GetBoolean() ? "Active" : "Suspended";
                }
            }

            return "Unknown";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get user status from Keycloak for user {UserId}", userId);
            return "Unknown";
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateUserStatusAsync(string userId, bool enabled, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminToken = await _keycloakAdminService.GetAdminAccessTokenAsync(cancellationToken);
            var baseUrl = GetKeycloakBaseUrl();
            var realm = _authOptions.Realm;
            var userUpdateUrl = $"{baseUrl}/admin/realms/{realm}/users/{userId}";

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var updatePayload = new { enabled };
            var jsonContent = JsonSerializer.Serialize(updatePayload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await httpClient.PutAsync(userUpdateUrl, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Failed to update user status in Keycloak. Status: {StatusCode}, Response: {Response}",
                response.StatusCode,
                errorContent);

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while updating user status in Keycloak for user {UserId}", userId);
            return false;
        }
    }

    private string GetKeycloakBaseUrl()
    {
        var authority = _authOptions.Authority;
        var baseUrl = authority;
        if (baseUrl.Contains("/realms/"))
        {
            baseUrl = baseUrl.Substring(0, baseUrl.IndexOf("/realms/"));
        }
        return baseUrl.TrimEnd('/');
    }

    private async Task<List<string>> GetUserRolesAsync(string baseUrl, string realm, string userId, string adminToken, CancellationToken cancellationToken)
    {
        var userRolesEndpoint = $"{baseUrl}/admin/realms/{realm}/users/{userId}/role-mappings/realm";
        using var rolesClient = _httpClientFactory.CreateClient();
        rolesClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var rolesResponse = await rolesClient.GetAsync(userRolesEndpoint, cancellationToken);
        var userRoles = new List<string>();

        if (rolesResponse.IsSuccessStatusCode)
        {
            var rolesContent = await rolesResponse.Content.ReadAsStringAsync(cancellationToken);
            var rolesArray = JsonSerializer.Deserialize<JsonElement[]>(rolesContent);
            if (rolesArray != null)
            {
                userRoles = rolesArray
                    .Where(r => r.TryGetProperty("name", out _))
                    .Select(r => r.GetProperty("name").GetString())
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToList()!;
            }
        }

        return userRoles;
    }
}
