using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

/// <remarks>
/// Excluded from code coverage: Entity Framework Core entity configuration.
/// Database schema configuration is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "EF Core entity configuration. Schema tested via integration tests.")]
public sealed class UserRewardConfiguration : IEntityTypeConfiguration<UserReward>
{
    public void Configure(EntityTypeBuilder<UserReward> builder)
    {
        builder.ToTable("UserRewards");

        builder.HasKey(ur => ur.Id);

        builder.Property(ur => ur.UserId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(ur => ur.Date)
            .IsRequired();

        builder.Property(ur => ur.TotalDistanceKm)
            .IsRequired()
            .HasPrecision(10, 2);

        builder.Property(ur => ur.Points)
            .IsRequired();

        builder.Property(ur => ur.CreatedOnUtc)
            .IsRequired();

        builder.HasIndex(ur => new { ur.UserId, ur.Date })
            .IsUnique();
        builder.HasIndex(ur => ur.Date);
    }
}

