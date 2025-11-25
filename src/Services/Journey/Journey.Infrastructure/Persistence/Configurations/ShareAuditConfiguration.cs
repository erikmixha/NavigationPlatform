using Journey.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Journey.Infrastructure.Persistence.Configurations;

/// <remarks>
/// Excluded from code coverage: Entity Framework Core entity configuration.
/// Database schema configuration is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "EF Core entity configuration. Schema tested via integration tests.")]
public sealed class ShareAuditConfiguration : IEntityTypeConfiguration<ShareAudit>
{
    public void Configure(EntityTypeBuilder<ShareAudit> builder)
    {
        builder.ToTable("ShareAudits");

        builder.HasKey(sa => sa.Id);

        builder.Property(sa => sa.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sa => sa.PerformedByUserId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(sa => sa.TargetUserId)
            .HasMaxLength(256);

        builder.Property(sa => sa.Timestamp)
            .IsRequired();

        builder.HasIndex(sa => sa.JourneyId);
        builder.HasIndex(sa => sa.PerformedByUserId);
        builder.HasIndex(sa => sa.Timestamp);
    }
}

