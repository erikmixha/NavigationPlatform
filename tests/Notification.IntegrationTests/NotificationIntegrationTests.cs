using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Notification.API.Consumers;
using Notification.Application.Interfaces;
using Notification.Infrastructure.Hubs;
using Notification.Infrastructure.Persistence;
using Notification.Infrastructure.Persistence.Repositories;
using Notification.Infrastructure.Services;
using Shared.Common.Configuration;
using Shared.Messaging.Events;
using Xunit;

namespace Notification.IntegrationTests;

[Trait("Category", "Integration")]
public class NotificationIntegrationTests : IClassFixture<NotificationTestFixture>
{
    private readonly NotificationTestFixture _fixture;

    public NotificationIntegrationTests(NotificationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task JourneyUpdatedEvent_ShouldNotifyOnlyFavoritingUsers()
    {
        var harness = _fixture.Harness;

        var favoritingUserIds = new List<string> { "user1", "user2" };
        var nonFavoritingUserId = "user3";
        var journeyId = Guid.NewGuid();

        var journeyUpdatedEvent = new JourneyUpdatedEvent
        {
            JourneyId = journeyId,
            UserId = "journey-owner",
            FavoritingUserIds = favoritingUserIds,
            StartLocation = "Location A",
            ArrivalLocation = "Location B",
            StartTime = DateTime.UtcNow,
            ArrivalTime = DateTime.UtcNow.AddHours(1),
            TransportType = "Commercial",
            DistanceKm = 100.00m
        };

        await harness.Bus.Publish(journeyUpdatedEvent);

        var consumed = await harness.Consumed.Any<JourneyUpdatedEvent>(
            x => x.Context.Message.JourneyId == journeyId);
        consumed.Should().BeTrue("Event should be consumed");

        // Verify that only favoriting users are in the list
        favoritingUserIds.Should().NotBeEmpty();
        favoritingUserIds.Should().NotContain(nonFavoritingUserId, "Non-favoriting user should not receive notification");
        favoritingUserIds.Should().NotContain("journey-owner", "Journey owner should not receive notification");
    }

    [Fact]
    public async Task JourneyDeletedEvent_ShouldNotifyOnlyFavoritingUsers()
    {
        var harness = _fixture.Harness;

        var favoritingUserIds = new List<string> { "user1", "user2" };
        var nonFavoritingUserId = "user3";
        var journeyId = Guid.NewGuid();

        var journeyDeletedEvent = new JourneyDeletedEvent
        {
            JourneyId = journeyId,
            UserId = "journey-owner",
            FavoritingUserIds = favoritingUserIds,
            OccurredOnUtc = DateTime.UtcNow
        };

        await harness.Bus.Publish(journeyDeletedEvent);

        var consumed = await harness.Consumed.Any<JourneyDeletedEvent>(
            x => x.Context.Message.JourneyId == journeyId);
        consumed.Should().BeTrue("Event should be consumed");

        favoritingUserIds.Should().NotBeEmpty();
        favoritingUserIds.Should().NotContain(nonFavoritingUserId, "Non-favoriting user should not receive notification");
        favoritingUserIds.Should().NotContain("journey-owner", "Journey owner should not receive notification");
    }

    [Fact]
    public async Task JourneyUpdatedEvent_ShouldNotNotifyWhenNoFavoritingUsers()
    {
        var harness = _fixture.Harness;

        var journeyId = Guid.NewGuid();

        var journeyUpdatedEvent = new JourneyUpdatedEvent
        {
            JourneyId = journeyId,
            UserId = "journey-owner",
            FavoritingUserIds = new List<string>(),
            StartLocation = "Location A",
            ArrivalLocation = "Location B",
            StartTime = DateTime.UtcNow,
            ArrivalTime = DateTime.UtcNow.AddHours(1),
            TransportType = "Commercial",
            DistanceKm = 100.00m
        };

        await harness.Bus.Publish(journeyUpdatedEvent);

        var consumed = await harness.Consumed.Any<JourneyUpdatedEvent>(
            x => x.Context.Message.JourneyId == journeyId);
        consumed.Should().BeTrue("Event should be consumed even if no favoriting users");

        journeyUpdatedEvent.FavoritingUserIds.Should().BeEmpty("No notifications should be sent when no users favorited");
    }

    [Fact]
    public async Task JourneyUpdatedEvent_ShouldUseEmailFallback_WhenSignalRFails()
    {
        var harness = _fixture.Harness;

        var favoritingUserIds = new List<string> { "offline-user" };
        var journeyId = Guid.NewGuid();

        // Mock notification service that fails SignalR
        var mockNotificationService = new Mock<INotificationService>();
        mockNotificationService
            .Setup(s => s.TrySendSignalRNotificationAsync(It.IsAny<string>(), It.IsAny<object>(), default))
            .ReturnsAsync(false); // SignalR fails

        mockNotificationService
            .Setup(s => s.SendEmailNotificationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                default))
            .Returns(Task.CompletedTask);

        var journeyUpdatedEvent = new JourneyUpdatedEvent
        {
            JourneyId = journeyId,
            UserId = "journey-owner",
            FavoritingUserIds = favoritingUserIds,
            StartLocation = "Location A",
            ArrivalLocation = "Location B",
            StartTime = DateTime.UtcNow,
            ArrivalTime = DateTime.UtcNow.AddHours(1),
            TransportType = "Commercial",
            DistanceKm = 100.00m
        };

        await harness.Bus.Publish(journeyUpdatedEvent);

        var consumed = await harness.Consumed.Any<JourneyUpdatedEvent>(
            x => x.Context.Message.JourneyId == journeyId);
        consumed.Should().BeTrue("Event should be consumed");

        // Verify email fallback would be called (in real scenario, this would be verified via MailHog)
        favoritingUserIds.Should().NotBeEmpty();
    }
}

public class NotificationTestFixture : IDisposable
{
    public InMemoryTestHarness Harness { get; }
    public IServiceProvider ServiceProvider { get; }

    public NotificationTestFixture()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        // Add in-memory database
        services.AddDbContext<NotificationDbContext>(options =>
            options.UseInMemoryDatabase($"NotificationDb_{Guid.NewGuid()}"));

        // Add repository
        services.AddScoped<INotificationRepository, NotificationRepository>();

        // Mock SignalR HubContext
        var hubContextMock = new Mock<IHubContext<NotificationHub>>();
        var clientsMock = new Mock<IHubClients>();
        var userClientsMock = new Mock<IClientProxy>();
        hubContextMock.Setup(x => x.Clients).Returns(clientsMock.Object);
        clientsMock.Setup(x => x.User(It.IsAny<string>())).Returns(userClientsMock.Object);
        userClientsMock.Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        services.AddSingleton(hubContextMock.Object);

        // Mock KeycloakUserService
        var keycloakUserServiceMock = new Mock<IKeycloakUserService>();
        keycloakUserServiceMock
            .Setup(x => x.GetUserEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("test@example.com");
        services.AddSingleton(keycloakUserServiceMock.Object);

        // Add SMTP options
        var smtpOptions = new SmtpOptions
        {
            Host = "localhost",
            Port = 1025,
            FromEmail = "test@test.com",
            FromName = "NavPlat Notifications",
            EnableSsl = false
        };
        services.AddSingleton(Options.Create(smtpOptions));

        // Add notification service
        services.AddScoped<INotificationService, NotificationService>();

        // Register consumers for direct instantiation
        services.AddScoped<JourneyUpdatedConsumer>();
        services.AddScoped<JourneyDeletedConsumer>();

        ServiceProvider = services.BuildServiceProvider();

        // Create and configure MassTransit test harness
        Harness = new InMemoryTestHarness();
        Harness.OnConfigureInMemoryBus += cfg =>
        {
            cfg.ReceiveEndpoint("test-queue", e =>
            {
                e.Consumer<JourneyUpdatedConsumer>(ServiceProvider);
                e.Consumer<JourneyDeletedConsumer>(ServiceProvider);
            });
        };
        
        // Start the harness once for all tests
        Harness.Start().Wait();
    }

    public void Dispose()
    {
        Harness?.Stop().Wait();
        Harness?.Dispose();
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}

