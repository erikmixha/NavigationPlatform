using System.Diagnostics.CodeAnalysis;
using Gateway.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gateway.Infrastructure.Persistence;

/// <remarks>
/// Excluded from code coverage: Entity Framework DbContext.
/// Database interactions are tested via integration tests.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Entity Framework DbContext. Database interactions tested via integration tests.")]
public sealed class GatewayDbContext : DbContext
{
    public GatewayDbContext(DbContextOptions<GatewayDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserStatusAudit> UserStatusAudits => Set<UserStatusAudit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserStatusAudit>(entity =>
        {
            entity.ToTable("UserStatusAudits");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(256);
            entity.Property(e => e.PreviousStatus).IsRequired().HasMaxLength(50);
            entity.Property(e => e.NewStatus).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ChangedByUserId).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Timestamp);
        });

        base.OnModelCreating(modelBuilder);
    }
}

