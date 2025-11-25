using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notification.API.Consumers;
using Shared.Common.Configuration;

namespace Notification.API.Extensions;

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
    /// Adds MassTransit with RabbitMQ and configures consumers for domain events.
    /// </summary>
    public static IServiceCollection AddMessagingServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var rabbitMqOptions = configuration.GetSection(RabbitMQOptions.SectionName)
            .Get<RabbitMQOptions>() ?? new RabbitMQOptions();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<DailyGoalAchievedConsumer>();
            x.AddConsumer<JourneyUpdatedConsumer>();
            x.AddConsumer<JourneyDeletedConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqOptions.Host, "/", h =>
                {
                    h.Username(rabbitMqOptions.Username);
                    h.Password(rabbitMqOptions.Password);
                });

                cfg.ReceiveEndpoint("notification-daily-goal-achieved", e =>
                {
                    e.ConfigureConsumer<DailyGoalAchievedConsumer>(context);
                });

                cfg.ReceiveEndpoint("notification-journey-updated", e =>
                {
                    e.ConfigureConsumer<JourneyUpdatedConsumer>(context);
                });

                cfg.ReceiveEndpoint("notification-journey-deleted", e =>
                {
                    e.ConfigureConsumer<JourneyDeletedConsumer>(context);
                });
            });
        });

        return services;
    }
}

