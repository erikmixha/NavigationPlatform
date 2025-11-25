using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence;

/// <remarks>
/// Excluded from code coverage: Entity Framework DbContext.
/// Database interactions are tested via integration tests.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Entity Framework DbContext. Database interactions tested via integration tests.")]
public sealed class RewardDbContext : DbContext
{
    public RewardDbContext(DbContextOptions<RewardDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserReward> UserRewards => Set<UserReward>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RewardDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

