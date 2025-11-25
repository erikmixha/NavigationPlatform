using Journey.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Journey.Infrastructure.Persistence.Configurations;

/// <remarks>
/// Excluded from code coverage: Entity Framework Core entity configuration.
/// Database schema configuration is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "EF Core entity configuration. Schema tested via integration tests.")]
public sealed class PublicLinkConfiguration : IEntityTypeConfiguration<PublicLink>
{
    public void Configure(EntityTypeBuilder<PublicLink> builder)
    {
        builder.ToTable("PublicLinks");

        builder.HasKey(pl => pl.Id);

        builder.Property(pl => pl.Token)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(pl => pl.CreatedByUserId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(pl => pl.CreatedOnUtc)
            .IsRequired();

        builder.Property(pl => pl.IsRevoked)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(pl => pl.RevokedOnUtc);

        builder.HasOne(pl => pl.Journey)
            .WithMany()
            .HasForeignKey(pl => pl.JourneyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(pl => pl.Token)
            .IsUnique();
        builder.HasIndex(pl => pl.JourneyId);
        builder.HasIndex(pl => new { pl.IsRevoked, pl.Token });
    }
}

