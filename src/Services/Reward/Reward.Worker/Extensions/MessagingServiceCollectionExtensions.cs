using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reward.Worker.Consumers;

namespace Reward.Worker.Extensions;

/// <summary>
/// Extension methods for configuring messaging services (MassTransit/RabbitMQ).
/// </summary>
/// <remarks>
/// Excluded from code coverage: Extension methods for service configuration.
/// Configuration is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Extension methods for service configuration. Tested via integration tests.")]
public static class MessagingServiceCollectionExtensions
{
    /// <summary>
    /// Adds MassTransit with RabbitMQ and configures consumers for journey domain events.
    /// </summary>
    public static IServiceCollection AddMessagingServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<JourneyCreatedConsumer>();
            x.AddConsumer<JourneyUpdatedConsumer>();
            x.AddConsumer<JourneyDeletedConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
                {
                    h.Username(configuration["RabbitMQ:Username"] ?? "guest");
                    h.Password(configuration["RabbitMQ:Password"] ?? "guest");
                });

                cfg.ReceiveEndpoint("reward-journey-created", e =>
                {
                    e.ConfigureConsumer<JourneyCreatedConsumer>(context);
                });

                cfg.ReceiveEndpoint("reward-journey-updated", e =>
                {
                    e.ConfigureConsumer<JourneyUpdatedConsumer>(context);
                });

                cfg.ReceiveEndpoint("reward-journey-deleted", e =>
                {
                    e.ConfigureConsumer<JourneyDeletedConsumer>(context);
                });
            });
        });

        return services;
    }
}

