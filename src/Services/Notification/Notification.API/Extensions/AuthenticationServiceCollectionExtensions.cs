using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Shared.Common.Configuration;

namespace Notification.API.Extensions;

/// <summary>
/// Extension methods for configuring authentication services.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Extension methods for authentication configuration.
/// Authentication setup is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Extension methods for authentication configuration. Tested via integration tests.")]
public static class AuthenticationServiceCollectionExtensions
{
    /// <summary>
    /// Adds JWT Bearer authentication with SignalR token extraction support.
    /// </summary>
    public static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authOptionsSnapshot = configuration.GetSection(AuthenticationOptions.SectionName)
            .Get<AuthenticationOptions>() ?? new AuthenticationOptions();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authOptionsSnapshot.Authority;
                options.Audience = authOptionsSnapshot.Audience;
                options.RequireHttpsMetadata = false;

                var publicAuthority = authOptionsSnapshot.Authority?.Replace("keycloak", "localhost", StringComparison.OrdinalIgnoreCase);
                var validIssuers = new List<string> { authOptionsSnapshot.Authority! };
                if (!string.IsNullOrEmpty(publicAuthority) && !publicAuthority.Equals(authOptionsSnapshot.Authority, StringComparison.OrdinalIgnoreCase))
                {
                    validIssuers.Add(publicAuthority);
                }

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuers = validIssuers,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var path = context.HttpContext.Request.Path;

                        if (path.StartsWithSegments("/hubs"))
                        {
                            var accessToken = context.Request.Query["access_token"];
                            if (!string.IsNullOrEmpty(accessToken))
                            {
                                context.Token = accessToken;
                            }
                            else
                            {
                                var authHeader = context.Request.Headers["Authorization"].ToString();
                                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                                {
                                    context.Token = authHeader.Substring("Bearer ".Length).Trim();
                                }
                            }
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }
}

