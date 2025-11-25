using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Notification.Domain.Entities;
using NotificationEntity = Notification.Domain.Entities.Notification;

namespace Notification.Infrastructure.Persistence;

/// <remarks>
/// Excluded from code coverage: Entity Framework DbContext.
/// Database interactions are tested via integration tests.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Entity Framework DbContext. Database interactions tested via integration tests.")]
public sealed class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
        : base(options)
    {
    }

    public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

