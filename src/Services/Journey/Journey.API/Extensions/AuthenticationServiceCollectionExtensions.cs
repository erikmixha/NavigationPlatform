using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Journey.API.Extensions;

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
    /// Adds JWT Bearer authentication with Keycloak role extraction.
    /// </summary>
    public static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var authority = configuration["Authentication:Authority"];
                options.Authority = authority;
                options.Audience = configuration["Authentication:Audience"];
                options.RequireHttpsMetadata = false;

                var publicAuthority = authority?.Replace("keycloak", "localhost", StringComparison.OrdinalIgnoreCase);
                var validIssuers = new List<string> { authority! };
                if (!string.IsNullOrEmpty(publicAuthority) && !publicAuthority.Equals(authority, StringComparison.OrdinalIgnoreCase))
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
                    ClockSkew = TimeSpan.Zero,
                    RoleClaimType = "role"
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        ExtractRolesFromToken(context, configuration);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }

    private static void ExtractRolesFromToken(Microsoft.AspNetCore.Authentication.JwtBearer.TokenValidatedContext context, IConfiguration configuration)
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerHandler>>();
        var identity = context.Principal?.Identity as System.Security.Claims.ClaimsIdentity;

        if (identity == null)
        {
            return;
        }

        var rolesFound = new List<string>();

        ExtractRolesFromRealmAccess(context, identity, rolesFound, logger);
        ExtractRolesFromResourceAccess(context, configuration, identity, rolesFound, logger);

        logger.LogInformation("Journey API extracted roles for user: {Roles}", string.Join(", ", rolesFound));
    }

    private static void ExtractRolesFromRealmAccess(
        Microsoft.AspNetCore.Authentication.JwtBearer.TokenValidatedContext context,
        System.Security.Claims.ClaimsIdentity identity,
        List<string> rolesFound,
        ILogger logger)
    {
        var realmAccessClaim = context.Principal?.FindFirst("realm_access");
        if (realmAccessClaim != null && !string.IsNullOrEmpty(realmAccessClaim.Value))
        {
            try
            {
                var realmAccess = JsonSerializer.Deserialize<JsonElement>(realmAccessClaim.Value);
                if (realmAccess.TryGetProperty("roles", out var roles))
                {
                    foreach (var role in roles.EnumerateArray())
                    {
                        var roleValue = role.GetString();
                        if (!string.IsNullOrEmpty(roleValue) && !rolesFound.Contains(roleValue))
                        {
                            logger.LogInformation("Adding realm role: {Role}", roleValue);
                            identity.AddClaim(new System.Security.Claims.Claim("role", roleValue));
                            rolesFound.Add(roleValue);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to parse realm_access claim");
            }
        }
    }

    private static void ExtractRolesFromResourceAccess(
        Microsoft.AspNetCore.Authentication.JwtBearer.TokenValidatedContext context,
        IConfiguration configuration,
        System.Security.Claims.ClaimsIdentity identity,
        List<string> rolesFound,
        ILogger logger)
    {
        var resourceAccessClaim = context.Principal?.FindFirst("resource_access");
        if (resourceAccessClaim != null && !string.IsNullOrEmpty(resourceAccessClaim.Value))
        {
            try
            {
                var resourceAccess = JsonSerializer.Deserialize<JsonElement>(resourceAccessClaim.Value);
                var clientIds = new[] { configuration["Authentication:Audience"] ?? "navplat-api", "navplat-gateway" };
                foreach (var clientId in clientIds)
                {
                    if (resourceAccess.TryGetProperty(clientId, out var clientAccess))
                    {
                        if (clientAccess.TryGetProperty("roles", out var roles))
                        {
                            foreach (var role in roles.EnumerateArray())
                            {
                                var roleValue = role.GetString();
                                if (!string.IsNullOrEmpty(roleValue) && !rolesFound.Contains(roleValue))
                                {
                                    logger.LogInformation("Adding client role from {ClientId}: {Role}", clientId, roleValue);
                                    identity.AddClaim(new System.Security.Claims.Claim("role", roleValue));
                                    rolesFound.Add(roleValue);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to parse resource_access claim");
            }
        }
    }
}

