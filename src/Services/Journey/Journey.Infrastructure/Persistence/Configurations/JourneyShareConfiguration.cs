using Journey.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Journey.Infrastructure.Persistence.Configurations;

/// <remarks>
/// Excluded from code coverage: Entity Framework Core entity configuration.
/// Database schema configuration is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "EF Core entity configuration. Schema tested via integration tests.")]
public sealed class JourneyShareConfiguration : IEntityTypeConfiguration<JourneyShare>
{
    public void Configure(EntityTypeBuilder<JourneyShare> builder)
    {
        builder.ToTable("JourneyShares");

        builder.HasKey(js => js.Id);

        builder.Property(js => js.SharedWithUserId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(js => js.SharedByUserId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(js => js.SharedOnUtc)
            .IsRequired();

        builder.HasOne(js => js.Journey)
            .WithMany()
            .HasForeignKey(js => js.JourneyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(js => js.JourneyId);
        builder.HasIndex(js => js.SharedWithUserId);
        builder.HasIndex(js => new { js.JourneyId, js.SharedWithUserId })
            .IsUnique();
    }
}

