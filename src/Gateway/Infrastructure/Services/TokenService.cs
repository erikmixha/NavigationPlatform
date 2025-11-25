using System.Net.Http.Json;
using System.Text.Json;
using Gateway.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Common.Configuration;

namespace Gateway.Infrastructure.Services;

/// <summary>
/// Service for token management operations.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Infrastructure service for token management.
/// Token operations are tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Infrastructure service - token management tested via integration tests.")]
public sealed class TokenService : ITokenService
{
    private readonly HttpClient _httpClient;
    private readonly AuthenticationOptions _authOptions;
    private readonly ILogger<TokenService> _logger;

    public TokenService(
        HttpClient httpClient,
        IOptions<AuthenticationOptions> authOptions,
        ILogger<TokenService> logger)
    {
        _httpClient = httpClient;
        _authOptions = authOptions.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TokenResponse?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_authOptions.Authority) || string.IsNullOrEmpty(_authOptions.ClientId))
        {
            _logger.LogWarning("Keycloak configuration missing, cannot refresh token");
            return null;
        }

        var tokenEndpoint = $"{_authOptions.Authority.TrimEnd('/')}/protocol/openid-connect/token";

        var requestContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken),
            new KeyValuePair<string, string>("client_id", _authOptions.ClientId),
            new KeyValuePair<string, string>("client_secret", _authOptions.ClientSecret ?? string.Empty)
        });

        try
        {
            var response = await _httpClient.PostAsync(tokenEndpoint, requestContent, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                var tokenResponse = JsonSerializer.Deserialize<JsonElement>(jsonResponse);

                if (tokenResponse.TryGetProperty("access_token", out var accessToken) &&
                    tokenResponse.TryGetProperty("refresh_token", out var newRefreshToken) &&
                    tokenResponse.TryGetProperty("expires_in", out var expiresIn))
                {
                    return new TokenResponse
                    {
                        AccessToken = accessToken.GetString() ?? string.Empty,
                        RefreshToken = newRefreshToken.GetString() ?? refreshToken,
                        ExpiresIn = expiresIn.GetInt32()
                    };
                }
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Failed to refresh token. Status: {StatusCode}, Response: {Response}",
                response.StatusCode,
                errorContent);

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while refreshing token");
            return null;
        }
    }
}

