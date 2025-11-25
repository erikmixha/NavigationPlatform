using Gateway.Application.DTOs;
using Gateway.Application.Interfaces;
using Microsoft.Extensions.Options;
using Shared.Common.Configuration;

namespace Gateway.Application.Services;

/// <summary>
/// Implementation of authentication service.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Application service for authentication operations.
/// Authentication flow is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Application service - authentication operations tested via integration tests.")]
public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Shared.Common.Configuration.AuthenticationOptions _authOptions;
    private readonly ILogger<AuthenticationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationService"/> class.
    /// </summary>
    public AuthenticationService(
        IHttpClientFactory httpClientFactory,
        IOptions<Shared.Common.Configuration.AuthenticationOptions> authOptions,
        ILogger<AuthenticationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _authOptions = authOptions.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<DTOs.TokenResponse?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_authOptions.Authority) || string.IsNullOrEmpty(_authOptions.ClientId))
        {
            _logger.LogWarning("Keycloak configuration missing, cannot refresh token");
            return null;
        }

        var tokenEndpoint = $"{_authOptions.Authority.TrimEnd('/')}/protocol/openid-connect/token";

        using var httpClient = _httpClientFactory.CreateClient();

        var requestContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken),
            new KeyValuePair<string, string>("client_id", _authOptions.ClientId),
            new KeyValuePair<string, string>("client_secret", _authOptions.ClientSecret ?? string.Empty)
        });

        try
        {
            var response = await httpClient.PostAsync(tokenEndpoint, requestContent, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(jsonResponse);

                if (tokenResponse.TryGetProperty("access_token", out var accessToken) &&
                    tokenResponse.TryGetProperty("refresh_token", out var newRefreshToken) &&
                    tokenResponse.TryGetProperty("expires_in", out var expiresIn))
                {
                    return new DTOs.TokenResponse
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
