using System.Diagnostics.CodeAnalysis;
using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Options;
using Shared.Common.Configuration;

namespace Gateway.Middleware;

/// <remarks>
/// Excluded from code coverage: Middleware for rate limiting login attempts.
/// Rate limiting logic is tested via integration tests.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Middleware - rate limiting tested via integration tests.")]
public class LoginRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoginRateLimitMiddleware> _logger;
    private readonly RateLimitSettings _settings;
    private static readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimitCache = new();

    public LoginRateLimitMiddleware(
        RequestDelegate next,
        ILogger<LoginRateLimitMiddleware> logger,
        IOptions<RateLimitSettings> settings)
    {
        _next = next;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
       
        var hasAuthCookie = context.Request.Cookies.ContainsKey(".NavPlat.Auth");
        var isSigninCallback = context.Request.Path.StartsWithSegments("/signin-oidc");
        var isAuthenticated = context.User.Identity?.IsAuthenticated == true;
        
        if (context.Request.Path.StartsWithSegments("/api/auth/login") &&
            context.Request.Method == "GET" &&
            !isSigninCallback &&
            !isAuthenticated &&
            !hasAuthCookie)
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var rateLimitInfo = _rateLimitCache.GetOrAdd(ipAddress, _ => new RateLimitInfo());

            bool shouldBlock = false;
            int currentAttempts = 0;
            var window = TimeSpan.FromMinutes(_settings.WindowMinutes);
            lock (rateLimitInfo)
            {
                var now = DateTime.UtcNow;
                
                if (rateLimitInfo.WindowStart + window < now)
                {
                    rateLimitInfo.Attempts = 0;
                    rateLimitInfo.WindowStart = now;
                }

                if (rateLimitInfo.Attempts >= _settings.MaxLoginAttempts)
                {
                    shouldBlock = true;
                    currentAttempts = rateLimitInfo.Attempts;
                }
                else
                {
                    rateLimitInfo.Attempts++;
                    currentAttempts = rateLimitInfo.Attempts;
                }
            }

            if (shouldBlock)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for IP {IpAddress}. Attempts: {Attempts}",
                    ipAddress,
                    currentAttempts);

                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    type = "https://tools.ietf.org/html/rfc6585#section-4",
                    title = "Too Many Requests",
                    status = 429,
                    detail = $"Rate limit exceeded. Maximum {_settings.MaxLoginAttempts} attempts per {_settings.WindowMinutes} minute(s).",
                    traceId = context.TraceIdentifier
                });
                return;
            }
        }

        await _next(context);
    }

    private class RateLimitInfo
    {
        public int Attempts { get; set; }
        public DateTime WindowStart { get; set; } = DateTime.UtcNow;
    }
}

