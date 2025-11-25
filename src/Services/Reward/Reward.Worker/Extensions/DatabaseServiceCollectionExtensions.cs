using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reward.Infrastructure.Persistence;

namespace Reward.Worker.Extensions;

/// <summary>
/// Extension methods for configuring database services.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Extension methods for service configuration.
/// Configuration is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Extension methods for service configuration. Tested via integration tests.")]
public static class DatabaseServiceCollectionExtensions
{
    /// <summary>
    /// Adds database services including Entity Framework Core with PostgreSQL.
    /// </summary>
    public static IServiceCollection AddDatabaseServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<RewardDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("RewardDb")));

        return services;
    }
}

