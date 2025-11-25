using Journey.Domain.Events;
using Journey.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Shared.Common.Configuration;
using Shared.Messaging.Events;
using System.Text.Json;

namespace Journey.Infrastructure.BackgroundServices;

/// <remarks>
/// Excluded from code coverage: Background service for outbox pattern message publishing.
/// Background service execution is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Background service - outbox pattern tested via integration tests.")]
public sealed class OutboxPublisherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxPublisherService> _logger;
    private readonly OutboxSettings _settings;
    private static readonly TimeSpan DatabaseReadyCheckInterval = TimeSpan.FromSeconds(2);
    private static readonly int MaxDatabaseReadyRetries = 30;

    public OutboxPublisherService(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxPublisherService> logger,
        IOptions<OutboxSettings> settings)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Publisher Service starting, waiting for database migrations to complete...");

        // Wait for database to be ready (migrations completed)
        if (!await WaitForDatabaseReadyAsync(stoppingToken))
        {
            _logger.LogError("Database not ready after maximum retries. Outbox Publisher Service will not start.");
            return;
        }

        _logger.LogInformation("Outbox Publisher Service started and ready to process messages");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(TimeSpan.FromSeconds(_settings.ProcessingIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("Outbox Publisher Service stopped");
    }

    private async Task<bool> WaitForDatabaseReadyAsync(CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < MaxDatabaseReadyRetries; attempt++)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<JourneyDbContext>();

                var count = await context.OutboxMessages
                    .CountAsync(cancellationToken);

                _logger.LogInformation("Database migrations completed. OutboxMessages table is ready (found {Count} existing messages).", count);
                return true;
            }
            catch (PostgresException ex) when (ex.SqlState == "42P01")
            {
                // Table doesn't exist yet - migrations still running
                _logger.LogDebug("OutboxMessages table not found yet (attempt {Attempt}/{MaxAttempts}), waiting...", 
                    attempt + 1, MaxDatabaseReadyRetries);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking database readiness (attempt {Attempt}/{MaxAttempts}): {Error}", 
                    attempt + 1, MaxDatabaseReadyRetries, ex.Message);
            }

            await Task.Delay(DatabaseReadyCheckInterval, cancellationToken);
        }

        return false;
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<JourneyDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var messages = await context.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(_settings.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                var integrationEvent = await MapToIntegrationEventAsync(context, message, cancellationToken);

                if (integrationEvent is not null)
                {
                    await publishEndpoint.Publish(integrationEvent, cancellationToken);
                    _logger.LogInformation("Published event {EventType} with ID {EventId}", message.EventType, message.Id);
                }

                message.ProcessedOnUtc = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing outbox message {MessageId}", message.Id);
                message.Error = ex.Message;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<object?> MapToIntegrationEventAsync(
        JourneyDbContext context,
        Persistence.Entities.OutboxMessage message,
        CancellationToken cancellationToken)
    {
        return message.EventType switch
        {
            nameof(JourneyCreated) => await MapEventAsync<JourneyCreated, JourneyCreatedEvent>(context, message, cancellationToken),
            nameof(JourneyUpdated) => await MapEventAsync<JourneyUpdated, JourneyUpdatedEvent>(context, message, cancellationToken),
            nameof(JourneyDeleted) => await MapEventAsync<JourneyDeleted, JourneyDeletedEvent>(context, message, cancellationToken),
            _ => null
        };
    }

    private static async Task<object?> MapEventAsync<TDomain, TIntegration>(
        JourneyDbContext context,
        Persistence.Entities.OutboxMessage message,
        CancellationToken cancellationToken)
        where TDomain : class
    {
        var domainEvent = JsonSerializer.Deserialize<TDomain>(message.Payload);
        if (domainEvent is null) return null;

        return typeof(TIntegration).Name switch
        {
            nameof(JourneyCreatedEvent) => MapJourneyCreated(domainEvent as JourneyCreated),
            nameof(JourneyUpdatedEvent) => await MapJourneyUpdatedAsync(context, domainEvent as JourneyUpdated, cancellationToken),
            nameof(JourneyDeletedEvent) => await MapJourneyDeletedAsync(context, domainEvent as JourneyDeleted, cancellationToken),
            _ => null
        };
    }

    private static JourneyCreatedEvent? MapJourneyCreated(JourneyCreated? e) =>
        e is null ? null : new JourneyCreatedEvent
        {
            JourneyId = e.JourneyId,
            UserId = e.UserId,
            StartLocation = e.StartLocation,
            StartTime = e.StartTime,
            ArrivalLocation = e.ArrivalLocation,
            ArrivalTime = e.ArrivalTime,
            TransportType = e.TransportType.ToString(),
            DistanceKm = e.DistanceKm,
            OccurredOnUtc = e.OccurredOnUtc
        };

    private static async Task<JourneyUpdatedEvent?> MapJourneyUpdatedAsync(
        JourneyDbContext context,
        JourneyUpdated? e,
        CancellationToken cancellationToken)
    {
        if (e is null) return null;

        var favoritingUserIds = await context.JourneyFavorites
            .Where(f => f.JourneyId == e.JourneyId && f.UserId != e.UserId)
            .Select(f => f.UserId)
            .ToListAsync(cancellationToken);

        var sharedUserIds = await context.JourneyShares
            .Where(s => s.JourneyId == e.JourneyId && s.SharedWithUserId != e.UserId)
            .Select(s => s.SharedWithUserId)
            .ToListAsync(cancellationToken);

        // Combine both lists and remove duplicates
        var allNotificationUserIds = favoritingUserIds.Union(sharedUserIds).Distinct().ToList();

        return new JourneyUpdatedEvent
        {
            JourneyId = e.JourneyId,
            UserId = e.UserId,
            StartLocation = e.StartLocation,
            StartTime = e.StartTime,
            ArrivalLocation = e.ArrivalLocation,
            ArrivalTime = e.ArrivalTime,
            TransportType = e.TransportType.ToString(),
            DistanceKm = e.DistanceKm,
            OldDistanceKm = e.OldDistanceKm,
            OldStartTime = e.OldStartTime,
            FavoritingUserIds = favoritingUserIds,
            OccurredOnUtc = e.OccurredOnUtc
        };
    }

    private static async Task<JourneyDeletedEvent?> MapJourneyDeletedAsync(
        JourneyDbContext context,
        JourneyDeleted? e,
        CancellationToken cancellationToken)
    {
        if (e is null) return null;

        var favoritingUserIds = await context.JourneyFavorites
            .Where(f => f.JourneyId == e.JourneyId && f.UserId != e.UserId)
            .Select(f => f.UserId)
            .ToListAsync(cancellationToken);

        var sharedUserIds = await context.JourneyShares
            .Where(s => s.JourneyId == e.JourneyId && s.SharedWithUserId != e.UserId)
            .Select(s => s.SharedWithUserId)
            .ToListAsync(cancellationToken);

        // Combine both lists and remove duplicates
        var allNotificationUserIds = favoritingUserIds.Union(sharedUserIds).Distinct().ToList();

        return new JourneyDeletedEvent
        {
            JourneyId = e.JourneyId,
            UserId = e.UserId,
            StartLocation = e.StartLocation,
            StartTime = e.StartTime,
            ArrivalLocation = e.ArrivalLocation,
            DistanceKm = e.DistanceKm,
            FavoritingUserIds = allNotificationUserIds,
            OccurredOnUtc = e.OccurredOnUtc
        };
    }
}

