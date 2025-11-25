using System.Net.Http.Json;
using System.Text.Json;
using Gateway.Application.DTOs;
using Shared.Common.Configuration;
using Microsoft.Extensions.Options;

namespace Gateway.Infrastructure.Services;

/// <summary>
/// Service for interacting with Keycloak Admin API.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Infrastructure service for Keycloak Admin API.
/// External service integration is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Infrastructure service - Keycloak Admin API tested via integration tests.")]
public sealed class KeycloakAdminService
{
    private readonly HttpClient _httpClient;
    private readonly AuthenticationOptions _authOptions;
    private readonly KeycloakOptions _keycloakOptions;
    private readonly ILogger<KeycloakAdminService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeycloakAdminService"/> class.
    /// </summary>
    public KeycloakAdminService(
        HttpClient httpClient,
        IOptions<AuthenticationOptions> authOptions,
        IOptions<KeycloakOptions> keycloakOptions,
        ILogger<KeycloakAdminService> logger)
    {
        _httpClient = httpClient;
        _authOptions = authOptions.Value;
        _keycloakOptions = keycloakOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Gets an admin access token from Keycloak using client credentials flow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The admin access token.</returns>
    public async Task<string> GetAdminAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var realm = _authOptions.Realm;
            var tokenUrl = $"/realms/{realm}/protocol/openid-connect/token";
            var clientId = _keycloakOptions.AdminClientId;
            var clientSecret = _keycloakOptions.AdminClientSecret;

            var tokenRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret)
            });

            var response = await _httpClient.PostAsync(tokenUrl, tokenRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var tokenResponse = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            return tokenResponse.GetProperty("access_token").GetString() 
                ?? throw new InvalidOperationException("Failed to retrieve access token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to obtain Keycloak admin access token");
            throw;
        }
    }

    /// <summary>
    /// Revokes a refresh token in Keycloak.
    /// </summary>
    /// <param name="refreshToken">The refresh token to revoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var realm = _authOptions.Realm;
            var revocationUrl = $"/realms/{realm}/protocol/openid-connect/revoke";
            var clientId = _authOptions.ClientId;
            var clientSecret = _authOptions.ClientSecret;

            var revocationRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("token", refreshToken),
                new KeyValuePair<string, string>("token_type_hint", "refresh_token"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret)
            });

            var response = await _httpClient.PostAsync(revocationUrl, revocationRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Successfully revoked refresh token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke refresh token");
            throw;
        }
    }
}
