using Journey.Application.Interfaces;
using Journey.Infrastructure.BackgroundServices;
using Journey.Infrastructure.Consumers;
using Journey.Infrastructure.Persistence;
using Journey.Infrastructure.Persistence.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Journey.Infrastructure.Extensions;

/// <remarks>
/// Excluded from code coverage: Extension methods for service configuration.
/// Configuration is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Extension methods for service configuration. Tested via integration tests.")]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureLayer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<JourneyDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("JourneyDb"));
        });
        
        services.AddScoped<JourneyDbContext>(sp =>
        {
            var options = sp.GetRequiredService<DbContextOptions<JourneyDbContext>>();
            return new JourneyDbContext(options, sp);
        });

        services.AddScoped<IJourneyRepository, JourneyRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        var infrastructureAssembly = typeof(ServiceCollectionExtensions).Assembly;
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(infrastructureAssembly);
        });

        services.AddMassTransit(x =>
        {
            x.AddConsumer<DailyGoalAchievedConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
                {
                    h.Username(configuration["RabbitMQ:Username"] ?? "guest");
                    h.Password(configuration["RabbitMQ:Password"] ?? "guest");
                });

                cfg.ReceiveEndpoint("journey-daily-goal-achieved", e =>
                {
                    e.ConfigureConsumer<DailyGoalAchievedConsumer>(context);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddHostedService<OutboxPublisherService>();

        return services;
    }

    public static async Task ApplyMigrationsAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<JourneyDbContext>();
        await context.Database.MigrateAsync();
    }
}

