using Journey.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Journey.Infrastructure.Persistence.Configurations;

/// <remarks>
/// Excluded from code coverage: Entity Framework Core entity configuration.
/// Database schema configuration is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "EF Core entity configuration. Schema tested via integration tests.")]
public sealed class JourneyConfiguration : IEntityTypeConfiguration<Domain.Entities.Journey>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Journey> builder)
    {
        builder.ToTable("Journeys");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.UserId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(j => j.StartLocation)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(j => j.ArrivalLocation)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(j => j.StartTime)
            .IsRequired();

        builder.Property(j => j.ArrivalTime)
            .IsRequired();

        builder.Property(j => j.TransportType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(j => j.DistanceKm)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasConversion(
                d => d.Value,
                v => DistanceKm.Create(v).Value);

        builder.Property(j => j.IsDailyGoalAchieved)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(j => j.CreatedOnUtc)
            .IsRequired();

        builder.Property(j => j.UpdatedOnUtc);

        builder.HasIndex(j => j.UserId);
        builder.HasIndex(j => j.StartTime);
        builder.HasIndex(j => new { j.UserId, j.StartTime });

        builder.Ignore(j => j.DomainEvents);
    }
}

