using System.Diagnostics.CodeAnalysis;
using Journey.Infrastructure.Persistence.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Shared.Common.Abstractions;

namespace Journey.Infrastructure.Persistence;

/// <remarks>
/// Excluded from code coverage: Entity Framework DbContext.
/// Database interactions are tested via integration tests.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Entity Framework DbContext. Database interactions tested via integration tests.")]
public sealed class JourneyDbContext : DbContext
{
    private readonly IServiceProvider? _serviceProvider;

    public JourneyDbContext(DbContextOptions<JourneyDbContext> options)
        : base(options)
    {
    }

    public JourneyDbContext(DbContextOptions<JourneyDbContext> options, IServiceProvider serviceProvider)
        : base(options)
    {
        _serviceProvider = serviceProvider;
    }

    public DbSet<Domain.Entities.Journey> Journeys => Set<Domain.Entities.Journey>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<Domain.Entities.JourneyShare> JourneyShares => Set<Domain.Entities.JourneyShare>();
    public DbSet<Domain.Entities.JourneyFavorite> JourneyFavorites => Set<Domain.Entities.JourneyFavorite>();
    public DbSet<Domain.Entities.PublicLink> PublicLinks => Set<Domain.Entities.PublicLink>();
    public DbSet<Domain.Entities.ShareAudit> ShareAudits => Set<Domain.Entities.ShareAudit>();
    public DbSet<Domain.Entities.MonthlyDistanceReadModel> MonthlyDistanceReadModels => Set<Domain.Entities.MonthlyDistanceReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JourneyDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var domainEvents = ChangeTracker
            .Entries<IAggregateRoot>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Any())
            .SelectMany(e =>
            {
                var events = e.DomainEvents.ToList();
                e.ClearDomainEvents();
                return events;
            })
            .ToList();

        if (_serviceProvider is not null)
        {
            var mediator = _serviceProvider.GetService<IMediator>();
            if (mediator is not null)
            {
                foreach (var domainEvent in domainEvents)
                {
                    await mediator.Publish(domainEvent, cancellationToken);
                }
            }
        }

        foreach (var domainEvent in domainEvents)
        {
            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = domainEvent.GetType().Name,
                Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                OccurredOnUtc = domainEvent.OccurredOnUtc,
            };

            OutboxMessages.Add(outboxMessage);
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}

