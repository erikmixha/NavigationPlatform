using Microsoft.EntityFrameworkCore;
using Reward.Infrastructure.Persistence;
using Serilog;

namespace Reward.Worker.Extensions;

/// <summary>
/// Extension methods for configuring the host application.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Extension methods for application configuration.
/// Configuration is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Extension methods for application configuration. Tested via integration tests.")]
public static class HostExtensions
{
    /// <summary>
    /// Applies database migrations asynchronously.
    /// </summary>
    public static async Task ApplyDatabaseMigrationsAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        await context.Database.MigrateAsync();
    }
}
