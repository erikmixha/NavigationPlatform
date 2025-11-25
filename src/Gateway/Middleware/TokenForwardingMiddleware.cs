using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;

namespace Gateway.Middleware;

/// <remarks>
/// Excluded from code coverage: Middleware for forwarding authentication tokens.
/// Token forwarding logic is tested via integration tests.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Middleware - token forwarding tested via integration tests.")]
public class TokenForwardingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TokenForwardingMiddleware> _logger;

    public TokenForwardingMiddleware(
        RequestDelegate next,
        ILogger<TokenForwardingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if ((context.Request.Path.StartsWithSegments("/api") && 
             !context.Request.Path.StartsWithSegments("/api/auth")) ||
            context.Request.Path.StartsWithSegments("/hubs"))
        {
            var authenticateResult = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            if (authenticateResult.Succeeded && authenticateResult.Principal != null)
            {
                var accessToken = authenticateResult.Properties?.GetTokenValue("access_token")
                    ?? authenticateResult.Properties?.GetTokenValue(".Token.access_token");
                
                if (string.IsNullOrEmpty(accessToken) && authenticateResult.Properties?.Items != null)
                {
                    authenticateResult.Properties.Items.TryGetValue("access_token", out var token1);
                    authenticateResult.Properties.Items.TryGetValue(".Token.access_token", out var token2);
                    accessToken = token1 ?? token2;
                }
                
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Request.Headers["Authorization"] = $"Bearer {accessToken}";
                    context.User = authenticateResult.Principal;
                    _logger.LogInformation("Forwarding access token to backend service for path {Path}", context.Request.Path);
                }
                else
                {
                    var availableKeys = authenticateResult.Properties?.Items.Keys ?? Enumerable.Empty<string>();
                    _logger.LogWarning(
                        "No access token found in authentication properties for path {Path}. Available keys: {Keys}",
                        context.Request.Path,
                        string.Join(", ", availableKeys));
                }
            }
            else if (context.User.Identity?.IsAuthenticated == true)
            {
               
                var authResult = await context.AuthenticateAsync();
                var accessToken = authResult.Properties?.GetTokenValue("access_token")
                    ?? authResult.Properties?.GetTokenValue(".Token.access_token");
                
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Request.Headers["Authorization"] = $"Bearer {accessToken}";
                    _logger.LogDebug("Forwarding access token from authenticated user for path {Path}", context.Request.Path);
                }
            }
        }

        await _next(context);
    }
}

