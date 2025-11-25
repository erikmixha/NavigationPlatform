using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Common.Configuration;

namespace Notification.Infrastructure.Services;

public interface IKeycloakUserService
{
    Task<string?> GetUserEmailAsync(string userId, CancellationToken cancellationToken = default);
}

/// <remarks>
/// Excluded from code coverage: Infrastructure service for Keycloak user operations.
/// External service integration is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Infrastructure service - Keycloak integration tested via integration tests.")]
public sealed class KeycloakUserService : IKeycloakUserService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeycloakUserService> _logger;
    private string? _cachedAdminToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public KeycloakUserService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<KeycloakUserService> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string?> GetUserEmailAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var authority = _configuration["Authentication:Authority"];
            var realm = _configuration["Authentication:Realm"] ?? "navplat";
            var adminClientId = _configuration["Keycloak:AdminClientId"];
            var adminClientSecret = _configuration["Keycloak:AdminClientSecret"];

            if (string.IsNullOrEmpty(authority) || string.IsNullOrEmpty(adminClientId))
            {
                _logger.LogWarning("Keycloak admin configuration missing, cannot retrieve user email");
                return null;
            }

            // Extract base URL from authority
            var baseUrl = authority;
            if (baseUrl.Contains("/realms/"))
            {
                baseUrl = baseUrl.Substring(0, baseUrl.IndexOf("/realms/"));
            }
            baseUrl = baseUrl.TrimEnd('/');

            // Get admin token (with caching)
            var adminToken = await GetAdminTokenAsync(baseUrl, realm, adminClientId, adminClientSecret ?? string.Empty, cancellationToken);
            if (string.IsNullOrEmpty(adminToken))
            {
                _logger.LogWarning("Failed to obtain admin token for user email lookup");
                return null;
            }

            // Get user from Keycloak
            var userEndpoint = $"{baseUrl}/admin/realms/{realm}/users/{userId}";
            using var request = new HttpRequestMessage(HttpMethod.Get, userEndpoint);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get user {UserId} from Keycloak. Status: {StatusCode}", userId, response.StatusCode);
                return null;
            }

            var userContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var user = JsonSerializer.Deserialize<JsonElement>(userContent);

            if (user.TryGetProperty("email", out var emailProp))
            {
                var email = emailProp.GetString();
                _logger.LogInformation("Retrieved email {Email} for user {UserId}", email, userId);
                return email;
            }

            _logger.LogWarning("User {UserId} does not have an email in Keycloak", userId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving email for user {UserId}", userId);
            return null;
        }
    }

    private async Task<string?> GetAdminTokenAsync(string baseUrl, string realm, string clientId, string clientSecret, CancellationToken cancellationToken)
    {
        // Return cached token if still valid (with 5 minute buffer)
        if (!string.IsNullOrEmpty(_cachedAdminToken) && _tokenExpiry > DateTime.UtcNow.AddMinutes(5))
        {
            return _cachedAdminToken;
        }

        try
        {
            var tokenUrl = $"{baseUrl}/realms/{realm}/protocol/openid-connect/token";
            using var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret)
            });

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var tokenResponse = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            var accessToken = tokenResponse.GetProperty("access_token").GetString();
            var expiresIn = tokenResponse.TryGetProperty("expires_in", out var expProp) 
                ? expProp.GetInt32() 
                : 300; // Default to 5 minutes

            _cachedAdminToken = accessToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn);

            return accessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to obtain Keycloak admin token");
            return null;
        }
    }
}

