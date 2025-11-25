using Gateway.Application.Interfaces;
using Gateway.Application.Services;
using Gateway.Infrastructure.Persistence;
using Gateway.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MassTransit;
using Shared.Common.Behaviors;

namespace Gateway.Extensions;

/// <summary>
/// Extension methods for configuring Gateway-specific services.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Extension methods for service configuration.
/// Configuration is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Extension methods for service configuration. Tested via integration tests.")]
public static class GatewayServiceCollectionExtensions
{
    /// <summary>
    /// Adds Gateway-specific services including database context, HTTP clients, and messaging.
    /// </summary>
    public static IServiceCollection AddGatewayServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<GatewayDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("GatewayDb")));

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GatewayServiceCollectionExtensions).Assembly));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddHttpClient();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IKeycloakUserService, KeycloakUserService>();
        services.AddScoped<IUserStatusAuditService, UserStatusAuditService>();

        services.AddHttpClient<KeycloakAdminService>((serviceProvider, client) =>
        {
            var authority = configuration["Authentication:Authority"] ?? "http://keycloak:8080";

            var baseUrl = authority;
            if (authority.Contains("/realms/"))
            {
                baseUrl = authority.Substring(0, authority.IndexOf("/realms/"));
            }

            if (!baseUrl.EndsWith("/"))
            {
                baseUrl += "/";
            }

            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("KeycloakAdminService");
            logger.LogInformation("Configuring KeycloakAdminService HttpClient with BaseAddress: {BaseAddress}", baseUrl);

            client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
                {
                    h.Username(configuration["RabbitMQ:Username"] ?? "guest");
                    h.Password(configuration["RabbitMQ:Password"] ?? "guest");
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
