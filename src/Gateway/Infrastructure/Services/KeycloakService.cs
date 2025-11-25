using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Gateway.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Common.Configuration;

namespace Gateway.Infrastructure.Services;

/// <summary>
/// Service for Keycloak administration operations.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Infrastructure service for Keycloak integration.
/// External service integration is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Infrastructure service - Keycloak integration tested via integration tests.")]
public sealed class KeycloakService : IKeycloakService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly AuthenticationOptions _authOptions;
    private readonly ILogger<KeycloakService> _logger;

    public KeycloakService(
        HttpClient httpClient,
        IConfiguration configuration,
        IOptions<AuthenticationOptions> authOptions,
        ILogger<KeycloakService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _authOptions = authOptions.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> GetUserStatusAsync(string userId, CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync(cancellationToken);
        if (string.IsNullOrEmpty(adminToken))
        {
            return "Unknown";
        }

        var baseUrl = GetBaseUrl();
        var realm = _authOptions.Realm;
        var userEndpoint = $"{baseUrl}/admin/realms/{realm}/users/{userId}";

        using var client = _httpClient;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        try
        {
            var response = await client.GetAsync(userEndpoint, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var userContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var user = JsonSerializer.Deserialize<JsonElement>(userContent);

                if (user.TryGetProperty("enabled", out var enabled))
                {
                    return enabled.GetBoolean() ? "Active" : "Suspended";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get user status from Keycloak for user {UserId}", userId);
        }

        return "Unknown";
    }

    /// <inheritdoc />
    public async Task<bool> UpdateUserStatusAsync(string userId, bool enabled, CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync(cancellationToken);
        if (string.IsNullOrEmpty(adminToken))
        {
            _logger.LogError("Failed to obtain Keycloak admin token");
            return false;
        }

        var baseUrl = GetBaseUrl();
        var realm = _authOptions.Realm;
        var userUpdateUrl = $"{baseUrl}/admin/realms/{realm}/users/{userId}";

        using var client = _httpClient;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var updatePayload = new { enabled };
        var jsonContent = JsonSerializer.Serialize(updatePayload);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await client.PutAsync(userUpdateUrl, content, cancellationToken);

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

    /// <inheritdoc />
    public async Task<IEnumerable<KeycloakUserInfo>> GetUsersWithRolesAsync(CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync(cancellationToken);
        if (string.IsNullOrEmpty(adminToken))
        {
            _logger.LogWarning("Failed to obtain Keycloak admin token");
            return Enumerable.Empty<KeycloakUserInfo>();
        }

        var baseUrl = GetBaseUrl();
        var realm = _authOptions.Realm;
        var usersEndpoint = $"{baseUrl}/admin/realms/{realm}/users";

        using var client = _httpClient;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await client.GetAsync(usersEndpoint, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to get users from Keycloak. Status: {StatusCode}", response.StatusCode);
            return Enumerable.Empty<KeycloakUserInfo>();
        }

        var usersContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var users = JsonSerializer.Deserialize<JsonElement[]>(usersContent);

        if (users == null)
        {
            return Enumerable.Empty<KeycloakUserInfo>();
        }

        var result = new List<KeycloakUserInfo>();

        foreach (var user in users)
        {
            var userId = user.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
            if (string.IsNullOrEmpty(userId))
            {
                continue;
            }

            var roles = await GetUserRolesAsync(baseUrl, realm, userId, adminToken, cancellationToken);

            var username = user.TryGetProperty("username", out var usernameProp) ? usernameProp.GetString() : null;
            var email = user.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;
            var firstName = user.TryGetProperty("firstName", out var firstNameProp) ? firstNameProp.GetString() : null;
            var lastName = user.TryGetProperty("lastName", out var lastNameProp) ? lastNameProp.GetString() : null;
            var enabled = user.TryGetProperty("enabled", out var enabledProp) && enabledProp.GetBoolean();

            result.Add(new KeycloakUserInfo
            {
                UserId = userId,
                Username = username ?? string.Empty,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Enabled = enabled,
                Roles = roles
            });
        }

        return result;
    }

    private async Task<IReadOnlyList<string>> GetUserRolesAsync(
        string baseUrl,
        string realm,
        string userId,
        string adminToken,
        CancellationToken cancellationToken)
    {
        var userRolesEndpoint = $"{baseUrl}/admin/realms/{realm}/users/{userId}/role-mappings/realm";

        using var rolesClient = _httpClient;
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
                    .Select(r => r.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToList()!;
            }
        }

        return userRoles;
    }

    private async Task<string?> GetAdminTokenAsync(CancellationToken cancellationToken)
    {
        var realm = _authOptions.Realm;
        var adminClientId = _configuration["Keycloak:AdminClientId"];
        var adminClientSecret = _configuration["Keycloak:AdminClientSecret"];

        if (string.IsNullOrEmpty(adminClientId))
        {
            return null;
        }

        var baseUrl = GetBaseUrl();
        var tokenUrl = $"{baseUrl}/realms/{realm}/protocol/openid-connect/token";

        using var client = _httpClient;

        var requestContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", adminClientId),
            new KeyValuePair<string, string>("client_secret", adminClientSecret ?? string.Empty)
        });

        try
        {
            var response = await client.PostAsync(tokenUrl, requestContent, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                var tokenResponse = JsonSerializer.Deserialize<JsonElement>(jsonResponse);

                if (tokenResponse.TryGetProperty("access_token", out var accessToken))
                {
                    return accessToken.GetString();
                }
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Failed to obtain Keycloak admin token. Status: {StatusCode}, Response: {Response}",
                response.StatusCode,
                errorContent);

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while obtaining Keycloak admin token");
            return null;
        }
    }

    private string GetBaseUrl()
    {
        var authority = _authOptions.Authority;
        var baseUrl = authority;

        if (baseUrl.Contains("/realms/"))
        {
            baseUrl = baseUrl.Substring(0, baseUrl.IndexOf("/realms/"));
        }

        return baseUrl.TrimEnd('/');
    }
}

