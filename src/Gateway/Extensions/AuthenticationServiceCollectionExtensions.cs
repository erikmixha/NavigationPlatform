using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Shared.Common.Configuration;
using System.Security.Claims;

namespace Gateway.Extensions;

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
    /// Adds authentication services with Cookie and OpenIdConnect schemes.
    /// </summary>
    public static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<Gateway.Infrastructure.Authentication.DistributedCacheTicketStore>();

        var authOptions = configuration.GetSection(Shared.Common.Configuration.AuthenticationOptions.SectionName)
            .Get<Shared.Common.Configuration.AuthenticationOptions>() ?? new Shared.Common.Configuration.AuthenticationOptions();
        var frontendOptions = configuration.GetSection(Gateway.Configuration.FrontendOptions.SectionName)
            .Get<Gateway.Configuration.FrontendOptions>() ?? new Gateway.Configuration.FrontendOptions();

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie(ConfigureCookieAuthentication(services))
        .AddOpenIdConnect(ConfigureOpenIdConnect(authOptions, frontendOptions));

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        });

        return services;
    }

    private static Action<CookieAuthenticationOptions> ConfigureCookieAuthentication(IServiceCollection services)
    {
        return options =>
        {
            options.Cookie.Name = ".NavPlat.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;

            var serviceProvider = services.BuildServiceProvider();
            options.SessionStore = serviceProvider.GetRequiredService<Gateway.Infrastructure.Authentication.DistributedCacheTicketStore>();

            options.Events.OnValidatePrincipal = async context =>
            {
                var tokens = context.Properties.Items;
                if (tokens.TryGetValue(".Token.expires_at", out var exp) &&
                    exp != null &&
                    DateTime.Parse(exp) < DateTime.UtcNow)
                {
                    context.RejectPrincipal();
                    await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignOutAsync(context.HttpContext);
                }
            };
        };
    }

    private static Action<OpenIdConnectOptions> ConfigureOpenIdConnect(
        Shared.Common.Configuration.AuthenticationOptions authOptions,
        Gateway.Configuration.FrontendOptions frontendOptions)
    {
        return options =>
        {
            var publicAuthority = authOptions.PublicAuthority ?? authOptions.Authority;
            var internalAuthority = authOptions.Authority;

            options.Authority = publicAuthority;
            options.ClientId = authOptions.ClientId;
            options.ClientSecret = authOptions.ClientSecret;
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.UsePkce = true;
            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = false;
            options.RequireHttpsMetadata = false;
            options.CallbackPath = "/signin-oidc";

            ConfigureOpenIdConnectMetadata(options, internalAuthority);
            ConfigureOpenIdConnectScopes(options);
            ConfigureOpenIdConnectTokenValidation(options, internalAuthority, publicAuthority);
            ConfigureOpenIdConnectEvents(options, authOptions, frontendOptions);
        };
    }

    private static void ConfigureOpenIdConnectMetadata(OpenIdConnectOptions options, string internalAuthority)
    {
        var documentRetriever = new Microsoft.IdentityModel.Protocols.HttpDocumentRetriever
        {
            RequireHttps = false
        };

        var configRetriever = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfigurationRetriever();
        var metadataAddress = $"{internalAuthority}/.well-known/openid-configuration";

        options.ConfigurationManager = new Microsoft.IdentityModel.Protocols.ConfigurationManager<Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration>(
            metadataAddress,
            configRetriever,
            documentRetriever
        );
    }

    private static void ConfigureOpenIdConnectScopes(OpenIdConnectOptions options)
    {
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Scope.Add("roles");
    }

    private static void ConfigureOpenIdConnectTokenValidation(
        OpenIdConnectOptions options,
        string internalAuthority,
        string publicAuthority)
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = new[] { internalAuthority, publicAuthority },
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = "role"
        };
    }

    private static void ConfigureOpenIdConnectEvents(
        OpenIdConnectOptions options,
        Shared.Common.Configuration.AuthenticationOptions authOptions,
        Gateway.Configuration.FrontendOptions frontendOptions)
    {
        options.Events = new OpenIdConnectEvents
        {
            OnRedirectToIdentityProvider = context => HandleRedirectToIdentityProvider(context, authOptions),
            OnAuthorizationCodeReceived = context => HandleAuthorizationCodeReceived(context, authOptions),
            OnTokenValidated = context => HandleTokenValidated(context, authOptions, frontendOptions),
            OnAuthenticationFailed = context => HandleAuthenticationFailed(context, frontendOptions)
        };
    }

    private static Task HandleRedirectToIdentityProvider(
        RedirectContext context,
        Shared.Common.Configuration.AuthenticationOptions authOptions)
    {
        var publicAuthority = authOptions.PublicAuthority ?? authOptions.Authority;
        var internalAuthority = authOptions.Authority;

        if (!string.IsNullOrEmpty(publicAuthority) &&
            !string.IsNullOrEmpty(internalAuthority) &&
            !publicAuthority.Equals(internalAuthority, StringComparison.OrdinalIgnoreCase) &&
            context.ProtocolMessage.IssuerAddress != null)
        {
            context.ProtocolMessage.IssuerAddress = context.ProtocolMessage.IssuerAddress
                .Replace(internalAuthority, publicAuthority, StringComparison.OrdinalIgnoreCase);
        }

        var redirectUri = !string.IsNullOrEmpty(authOptions.RedirectUri)
            ? authOptions.RedirectUri
            : "http://localhost:5000/signin-oidc";
        context.ProtocolMessage.RedirectUri = redirectUri;

        context.ProtocolMessage.SetParameter("code_challenge_method", "S256");

        if (!string.IsNullOrEmpty(authOptions.Audience))
        {
            context.ProtocolMessage.SetParameter("audience", authOptions.Audience);
        }

        return Task.CompletedTask;
    }

    private static Task HandleAuthorizationCodeReceived(
        AuthorizationCodeReceivedContext context,
        Shared.Common.Configuration.AuthenticationOptions authOptions)
    {
        if (!string.IsNullOrEmpty(authOptions.Audience))
        {
            context.TokenEndpointRequest.SetParameter("audience", authOptions.Audience);
        }
        return Task.CompletedTask;
    }

    private static async Task HandleTokenValidated(
        TokenValidatedContext context,
        Shared.Common.Configuration.AuthenticationOptions authOptions,
        Gateway.Configuration.FrontendOptions frontendOptions)
    {
        var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Gateway.Authentication");

        var userInfo = ExtractUserInfo(context.Principal);
        logger.LogInformation("OnTokenValidated - UserId: {UserId}, Username: {Username}, Email: {Email}",
            userInfo.UserId, userInfo.Username, userInfo.Email);

        var identity = context.Principal?.Identity as ClaimsIdentity;
        if (identity == null)
        {
            return;
        }

        logger.LogInformation("Extracting roles for user {UserId}. All claims: {Claims}",
            userInfo.UserId,
            string.Join(", ", context.Principal?.Claims.Select(c => $"{c.Type}={c.Value}") ?? Array.Empty<string>()));

        var rolesFound = new List<string>();

        ExtractRolesFromAccessToken(context, identity, rolesFound, authOptions, logger);
        ExtractDirectRoleClaims(context, identity, rolesFound, logger);
        ExtractRolesFromIdToken(context, identity, rolesFound, authOptions, logger);

        logger.LogInformation("Final roles for user {UserId}: {Roles}", userInfo.UserId, string.Join(", ", rolesFound));

        SetRedirectUri(context, frontendOptions);
        LogUserLogin(userInfo, rolesFound, logger);
    }

    private static (string? UserId, string Username, string? Email) ExtractUserInfo(ClaimsPrincipal? principal)
    {
        var userId = principal?.FindFirst("sub")?.Value
            ?? principal?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        var email = principal?.FindFirst("email")?.Value;
        var name = principal?.FindFirst("name")?.Value;
        var username = principal?.FindFirst("preferred_username")?.Value ??
                      name ??
                      email ??
                      userId ??
                      string.Empty;

        return (userId, username, email);
    }

    private static void ExtractRolesFromAccessToken(
        TokenValidatedContext context,
        ClaimsIdentity identity,
        List<string> rolesFound,
        Shared.Common.Configuration.AuthenticationOptions authOptions,
        ILogger logger)
    {
        var accessToken = context.TokenEndpointResponse?.AccessToken;
        if (string.IsNullOrEmpty(accessToken))
        {
            return;
        }

        try
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(accessToken);

            logger.LogInformation("Decoded access token. Claims: {Claims}",
                string.Join(", ", jsonToken.Claims.Select(c => $"{c.Type}={c.Value}")));

            ExtractRealmRolesFromToken(jsonToken, identity, rolesFound, logger, "access token");
            ExtractClientRolesFromToken(jsonToken, identity, rolesFound, authOptions.ClientId, logger, "access token");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to decode access token for role extraction");
        }
    }

    private static void ExtractDirectRoleClaims(
        TokenValidatedContext context,
        ClaimsIdentity identity,
        List<string> rolesFound,
        ILogger logger)
    {
        var directRoleClaims = context.Principal?.FindAll("role").Select(c => c.Value).ToList();
        if (directRoleClaims != null && directRoleClaims.Any())
        {
            logger.LogInformation("Found direct role claims: {Roles}", string.Join(", ", directRoleClaims));
            foreach (var role in directRoleClaims)
            {
                if (!rolesFound.Contains(role))
                {
                    rolesFound.Add(role);
                }
            }
        }
    }

    private static void ExtractRolesFromIdToken(
        TokenValidatedContext context,
        ClaimsIdentity identity,
        List<string> rolesFound,
        Shared.Common.Configuration.AuthenticationOptions authOptions,
        ILogger logger)
    {
        ExtractRealmRolesFromIdToken(context, identity, rolesFound, logger);
        ExtractClientRolesFromIdToken(context, identity, rolesFound, authOptions, logger);
    }

    private static void ExtractRealmRolesFromIdToken(
        TokenValidatedContext context,
        ClaimsIdentity identity,
        List<string> rolesFound,
        ILogger logger)
    {
        var realmAccessClaim = context.Principal?.FindFirst("realm_access");
        if (realmAccessClaim == null || string.IsNullOrEmpty(realmAccessClaim.Value))
        {
            logger.LogInformation("No realm_access claim found");
            return;
        }

        logger.LogInformation("Found realm_access claim: {Value}", realmAccessClaim.Value);
        try
        {
            var realmAccess = JsonSerializer.Deserialize<JsonElement>(realmAccessClaim.Value);
            ExtractRolesFromRealmAccess(realmAccess, identity, rolesFound, logger, "ID token");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse realm_access roles");
        }
    }

    private static void ExtractClientRolesFromIdToken(
        TokenValidatedContext context,
        ClaimsIdentity identity,
        List<string> rolesFound,
        Shared.Common.Configuration.AuthenticationOptions authOptions,
        ILogger logger)
    {
        var resourceAccessClaim = context.Principal?.FindFirst("resource_access");
        if (resourceAccessClaim == null || string.IsNullOrEmpty(resourceAccessClaim.Value))
        {
            logger.LogInformation("No resource_access claim found");
            return;
        }

        logger.LogInformation("Found resource_access claim: {Value}", resourceAccessClaim.Value);
        try
        {
            var resourceAccess = JsonSerializer.Deserialize<JsonElement>(resourceAccessClaim.Value);
            logger.LogInformation("Looking for client {ClientId} in resource_access", authOptions.ClientId);

            if (resourceAccess.TryGetProperty(authOptions.ClientId, out var clientAccess))
            {
                ExtractRolesFromClientAccess(clientAccess, identity, rolesFound, logger, "ID token");
            }
            else
            {
                logger.LogInformation("Client {ClientId} not found in resource_access. Available clients: {Clients}",
                    authOptions.ClientId,
                    string.Join(", ", resourceAccess.EnumerateObject().Select(p => p.Name)));
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse resource_access roles for client {ClientId}", authOptions.ClientId);
        }
    }

    private static void ExtractRealmRolesFromToken(
        System.IdentityModel.Tokens.Jwt.JwtSecurityToken token,
        ClaimsIdentity identity,
        List<string> rolesFound,
        ILogger logger,
        string tokenSource)
    {
        var realmAccessClaim = token.Claims.FirstOrDefault(c => c.Type == "realm_access");
        if (realmAccessClaim == null)
        {
            return;
        }

        var realmAccess = JsonSerializer.Deserialize<JsonElement>(realmAccessClaim.Value);
        ExtractRolesFromRealmAccess(realmAccess, identity, rolesFound, logger, tokenSource);
    }

    private static void ExtractRolesFromRealmAccess(
        JsonElement realmAccess,
        ClaimsIdentity identity,
        List<string> rolesFound,
        ILogger logger,
        string source)
    {
        if (!realmAccess.TryGetProperty("roles", out var roles))
        {
            return;
        }

        foreach (var role in roles.EnumerateArray())
        {
            var roleValue = role.GetString();
            if (!string.IsNullOrEmpty(roleValue) && !rolesFound.Contains(roleValue))
            {
                logger.LogInformation("Adding realm role from {Source}: {Role}", source, roleValue);
                identity.AddClaim(new Claim("role", roleValue));
                rolesFound.Add(roleValue);
            }
        }
    }

    private static void ExtractClientRolesFromToken(
        System.IdentityModel.Tokens.Jwt.JwtSecurityToken token,
        ClaimsIdentity identity,
        List<string> rolesFound,
        string clientId,
        ILogger logger,
        string tokenSource)
    {
        var resourceAccessClaim = token.Claims.FirstOrDefault(c => c.Type == "resource_access");
        if (resourceAccessClaim == null)
        {
            return;
        }

        var resourceAccess = JsonSerializer.Deserialize<JsonElement>(resourceAccessClaim.Value);
        if (!resourceAccess.TryGetProperty(clientId, out var clientAccess))
        {
            return;
        }

        ExtractRolesFromClientAccess(clientAccess, identity, rolesFound, logger, tokenSource);
    }

    private static void ExtractRolesFromClientAccess(
        JsonElement clientAccess,
        ClaimsIdentity identity,
        List<string> rolesFound,
        ILogger logger,
        string source)
    {
        if (!clientAccess.TryGetProperty("roles", out var roles))
        {
            return;
        }

        foreach (var role in roles.EnumerateArray())
        {
            var roleValue = role.GetString();
            if (!string.IsNullOrEmpty(roleValue) && !rolesFound.Contains(roleValue))
            {
                logger.LogInformation("Adding client role from {Source}: {Role}", source, roleValue);
                identity.AddClaim(new Claim("role", roleValue));
                rolesFound.Add(roleValue);
            }
        }
    }

    private static void SetRedirectUri(TokenValidatedContext context, Gateway.Configuration.FrontendOptions frontendOptions)
    {
        var frontendUrl = frontendOptions.Url;
        var returnUrl = context.Properties.RedirectUri;

        if (string.IsNullOrEmpty(returnUrl) || !returnUrl.StartsWith(frontendUrl))
        {
            context.Properties.RedirectUri = frontendUrl;
        }
    }

    private static void LogUserLogin(
        (string? UserId, string Username, string? Email) userInfo,
        List<string> rolesFound,
        ILogger logger)
    {
        if (string.IsNullOrEmpty(userInfo.UserId))
        {
            logger.LogWarning("UserId is null, cannot save user to database");
            return;
        }

        var rolesString = rolesFound != null && rolesFound.Any()
            ? string.Join(",", rolesFound)
            : string.Empty;
        logger.LogInformation("User {UserId} ({Username}) logged in with roles: {Roles}",
            userInfo.UserId, userInfo.Username, rolesString);
    }

    private static Task HandleAuthenticationFailed(
        AuthenticationFailedContext context,
        Gateway.Configuration.FrontendOptions frontendOptions)
    {
        var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Gateway.Authentication");
        logger.LogError(context.Exception, "Authentication failed: {Error}", context.Exception?.Message);

        var referer = context.HttpContext.Request.Headers.Referer.ToString();
        if (!referer.Contains("error=auth_failed"))
        {
            context.Response.Redirect($"{frontendOptions.Url}/?error=auth_failed");
            context.HandleResponse();
        }
        else
        {
            context.Response.StatusCode = 401;
            context.HandleResponse();
        }

        return Task.CompletedTask;
    }
}
