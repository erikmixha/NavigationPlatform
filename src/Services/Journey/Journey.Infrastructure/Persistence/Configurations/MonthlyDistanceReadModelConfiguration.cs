using Journey.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Journey.Infrastructure.Persistence.Configurations;

/// <remarks>
/// Excluded from code coverage: Entity Framework Core entity configuration.
/// Database schema configuration is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "EF Core entity configuration. Schema tested via integration tests.")]
public sealed class MonthlyDistanceReadModelConfiguration : IEntityTypeConfiguration<MonthlyDistanceReadModel>
{
    public void Configure(EntityTypeBuilder<MonthlyDistanceReadModel> builder)
    {
        builder.ToTable("MonthlyDistanceReadModels");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.UserId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(m => m.Year)
            .IsRequired();

        builder.Property(m => m.Month)
            .IsRequired();

        builder.Property(m => m.TotalDistanceKm)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(m => m.JourneyCount)
            .IsRequired();

        builder.Property(m => m.LastUpdatedOnUtc)
            .IsRequired();

        builder.HasIndex(m => new { m.UserId, m.Year, m.Month })
            .IsUnique();

        builder.HasIndex(m => new { m.Year, m.Month });
    }
}

