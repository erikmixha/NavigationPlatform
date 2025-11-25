using Journey.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Journey.Infrastructure.Persistence.Configurations;

/// <remarks>
/// Excluded from code coverage: Entity Framework Core entity configuration.
/// Database schema configuration is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "EF Core entity configuration. Schema tested via integration tests.")]
public sealed class JourneyFavoriteConfiguration : IEntityTypeConfiguration<JourneyFavorite>
{
    public void Configure(EntityTypeBuilder<JourneyFavorite> builder)
    {
        builder.ToTable("JourneyFavorites");

        builder.HasKey(jf => jf.Id);

        builder.Property(jf => jf.UserId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(jf => jf.FavoritedOnUtc)
            .IsRequired();

        builder.HasOne(jf => jf.Journey)
            .WithMany()
            .HasForeignKey(jf => jf.JourneyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(jf => jf.JourneyId);
        builder.HasIndex(jf => jf.UserId);
        builder.HasIndex(jf => new { jf.JourneyId, jf.UserId })
            .IsUnique();
    }
}

