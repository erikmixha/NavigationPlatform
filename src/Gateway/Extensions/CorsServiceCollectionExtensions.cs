using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shared.Common.Configuration;

namespace Gateway.Extensions;

/// <summary>
/// Extension methods for configuring CORS services.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Extension methods for service configuration.
/// Configuration is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Extension methods for service configuration. Tested via integration tests.")]
public static class CorsServiceCollectionExtensions
{
    /// <summary>
    /// Adds CORS services with frontend and Keycloak callback policies.
    /// </summary>
    public static IServiceCollection AddCorsServices(this IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();
        var corsOptions = serviceProvider.GetRequiredService<IOptions<CorsOptions>>().Value;
        var environment = serviceProvider.GetRequiredService<IWebHostEnvironment>();

        services.AddCors(options =>
        {
            options.AddPolicy("FrontendCors", policy =>
            {
                if (environment.IsDevelopment() && corsOptions.AllowAnyOriginInDevelopment)
                {
                    policy.SetIsOriginAllowed(_ => true)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                }
                else if (corsOptions.AllowedOrigins.Length > 0)
                {
                    policy.WithOrigins(corsOptions.AllowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
                }
            });

            options.AddPolicy("KeycloakCallback", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        return services;
    }
}

