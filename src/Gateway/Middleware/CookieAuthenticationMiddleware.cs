using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Gateway.Middleware;

/// <remarks>
/// Excluded from code coverage: Middleware for cookie authentication.
/// Authentication flow is tested via integration tests.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Middleware - cookie authentication tested via integration tests.")]
public class CookieAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public CookieAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            var isPublicRoute = context.Request.Path.StartsWithSegments("/api/journeys/public");
            var isAuthRoute = context.Request.Path.StartsWithSegments("/api/auth");
            
            if (context.Request.Path.StartsWithSegments("/api") && 
                !isAuthRoute && !isPublicRoute)
            {
                var authenticateResult = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                
                if (!authenticateResult.Succeeded)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                        title = "Unauthorized",
                        status = 401,
                        detail = "Authentication required",
                        traceId = context.TraceIdentifier
                    });
                    return;
                }

                var accessTokenExpiry = authenticateResult.Properties?.GetTokenValue("expires_at");
                var refreshToken = authenticateResult.Properties?.GetTokenValue("refresh_token");
                
                if (accessTokenExpiry != null && DateTime.Parse(accessTokenExpiry) < DateTime.UtcNow)
                {
                    if (!string.IsNullOrEmpty(refreshToken))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                            title = "Unauthorized",
                            status = 401,
                            detail = "Token expired. Please refresh your token.",
                            traceId = context.TraceIdentifier,
                            requiresRefresh = true
                        });
                        return;
                    }
                    
                    await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme);
                    return;
                }
            }
        }

        await _next(context);
    }
}

