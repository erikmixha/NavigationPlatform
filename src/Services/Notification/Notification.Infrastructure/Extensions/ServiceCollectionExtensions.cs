using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notification.Application.Interfaces;
using Notification.Infrastructure.Persistence;
using Notification.Infrastructure.Persistence.Repositories;
using Notification.Infrastructure.Services;

namespace Notification.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring the Infrastructure layer services.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Extension methods for service configuration.
/// Configuration is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Extension methods for service configuration. Tested via integration tests.")]
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Infrastructure layer services including database context, repositories, and services.
    /// </summary>
    public static IServiceCollection AddInfrastructureLayer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<NotificationDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("NotificationDb"));
        });

        services.AddHttpClient();
        services.AddScoped<IKeycloakUserService, KeycloakUserService>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }

    /// <summary>
    /// Applies database migrations asynchronously.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        await context.Database.MigrateAsync();
    }
}
