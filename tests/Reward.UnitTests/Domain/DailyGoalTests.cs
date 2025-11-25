using FluentAssertions;
using Journey.Domain.Enums;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Reward.Domain.Entities;
using Reward.Infrastructure.Persistence;
using Reward.Worker.Consumers;
using Shared.Messaging.Events;
using Xunit;

namespace Reward.UnitTests.Domain;

public class DailyGoalTests : IDisposable
{
    private readonly RewardDbContext _context;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly Mock<ILogger<JourneyCreatedConsumer>> _loggerMock;
    private readonly JourneyCreatedConsumer _consumer;
    private const decimal DailyGoalKm = 20.0m;

    public DailyGoalTests()
    {
        var options = new DbContextOptionsBuilder<RewardDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new RewardDbContext(options);
        _publishEndpointMock = new Mock<IPublishEndpoint>();
        _loggerMock = new Mock<ILogger<JourneyCreatedConsumer>>();
        var rewardSettings = Microsoft.Extensions.Options.Options.Create(new Shared.Common.Configuration.RewardSettings
        {
            DailyGoalKm = 20.0m,
            PointsPerKm = 10
        });
        _consumer = new JourneyCreatedConsumer(_context, _publishEndpointMock.Object, _loggerMock.Object, rewardSettings);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DailyGoal_WhenTotalIs19_99Km_ShouldNotPublishGoalAchievedEvent()
    {
        // Arrange
        var userId = "test-user-1";
        var date = DateTime.UtcNow.Date;
        
        var journeyEvent = new JourneyCreatedEvent
        {
            JourneyId = Guid.NewGuid(),
            UserId = userId,
            StartLocation = "Start",
            StartTime = DateTime.UtcNow,
            ArrivalLocation = "End",
            ArrivalTime = DateTime.UtcNow.AddHours(1),
            TransportType = TransportType.Commercial.ToString(),
            DistanceKm = 19.99m
        };

        var context = Mock.Of<ConsumeContext<JourneyCreatedEvent>>(
            c => c.Message == journeyEvent);

        // Act
        await _consumer.Consume(context);

        // Assert
        var userReward = await _context.UserRewards
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.Date == date);

        userReward.Should().NotBeNull();
        userReward!.TotalDistanceKm.Should().Be(19.99m);
        
        // Verify that DailyGoalAchievedEvent was NOT published
        _publishEndpointMock.Verify(
            x => x.Publish(
                It.IsAny<DailyGoalAchievedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never,
            "Goal event should not be published when distance is below 20km");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DailyGoal_WhenTotalIsExactly20_00Km_ShouldPublishGoalAchievedEvent()
    {
        // Arrange
        var userId = "test-user-2";
        var date = DateTime.UtcNow.Date;
        
        var journeyEvent = new JourneyCreatedEvent
        {
            JourneyId = Guid.NewGuid(),
            UserId = userId,
            StartLocation = "Start",
            StartTime = DateTime.UtcNow,
            ArrivalLocation = "End",
            ArrivalTime = DateTime.UtcNow.AddHours(1),
            TransportType = TransportType.Cargo.ToString(),
            DistanceKm = 20.00m
        };

        var context = Mock.Of<ConsumeContext<JourneyCreatedEvent>>(
            c => c.Message == journeyEvent);

        // Act
        await _consumer.Consume(context);

        // Assert
        var userReward = await _context.UserRewards
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.Date == date);

        userReward.Should().NotBeNull();
        userReward!.TotalDistanceKm.Should().Be(20.00m);
        
        // Verify that DailyGoalAchievedEvent WAS published exactly once
        _publishEndpointMock.Verify(
            x => x.Publish(
                It.Is<DailyGoalAchievedEvent>(e => 
                    e.UserId == userId &&
                    e.Date == date &&
                    e.TotalDistanceKm == 20.00m &&
                    e.GoalDistanceKm == DailyGoalKm),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Goal event should be published exactly once when distance reaches 20km");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DailyGoal_WhenTotalIs20_01Km_ShouldPublishGoalAchievedEvent()
    {
        // Arrange
        var userId = "test-user-3";
        var date = DateTime.UtcNow.Date;
        
        var journeyEvent = new JourneyCreatedEvent
        {
            JourneyId = Guid.NewGuid(),
            UserId = userId,
            StartLocation = "Start",
            StartTime = DateTime.UtcNow,
            ArrivalLocation = "End",
            ArrivalTime = DateTime.UtcNow.AddHours(1),
            TransportType = TransportType.Private.ToString(),
            DistanceKm = 20.01m
        };

        var context = Mock.Of<ConsumeContext<JourneyCreatedEvent>>(
            c => c.Message == journeyEvent);

        // Act
        await _consumer.Consume(context);

        // Assert
        var userReward = await _context.UserRewards
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.Date == date);

        userReward.Should().NotBeNull();
        userReward!.TotalDistanceKm.Should().Be(20.01m);
        
        // Verify that DailyGoalAchievedEvent WAS published
        _publishEndpointMock.Verify(
            x => x.Publish(
                It.Is<DailyGoalAchievedEvent>(e => 
                    e.UserId == userId &&
                    e.Date == date &&
                    e.TotalDistanceKm == 20.01m &&
                    e.GoalDistanceKm == DailyGoalKm),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Goal event should be published when distance exceeds 20km");
    }

    

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DailyGoal_WhenGoalAchievedOnDifferentDays_ShouldPublishForEachDay()
    {
        // Arrange
        var userId = "test-user-5";
        var day1 = DateTime.UtcNow.Date;
        var day2 = DateTime.UtcNow.Date.AddDays(1);

        var journeyDay1 = new JourneyCreatedEvent
        {
            JourneyId = Guid.NewGuid(),
            UserId = userId,
            StartLocation = "Start",
            StartTime = new DateTime(day1.Year, day1.Month, day1.Day, 10, 0, 0, DateTimeKind.Utc),
            ArrivalLocation = "End",
            ArrivalTime = new DateTime(day1.Year, day1.Month, day1.Day, 11, 0, 0, DateTimeKind.Utc),
            TransportType = TransportType.Commercial.ToString(),
            DistanceKm = 25.00m
        };

        var journeyDay2 = new JourneyCreatedEvent
        {
            JourneyId = Guid.NewGuid(),
            UserId = userId,
            StartLocation = "Start",
            StartTime = new DateTime(day2.Year, day2.Month, day2.Day, 10, 0, 0, DateTimeKind.Utc),
            ArrivalLocation = "End",
            ArrivalTime = new DateTime(day2.Year, day2.Month, day2.Day, 11, 0, 0, DateTimeKind.Utc),
            TransportType = TransportType.Cargo.ToString(),
            DistanceKm = 22.00m
        };

        var context1 = Mock.Of<ConsumeContext<JourneyCreatedEvent>>(c => c.Message == journeyDay1);
        var context2 = Mock.Of<ConsumeContext<JourneyCreatedEvent>>(c => c.Message == journeyDay2);

        // Act
        await _consumer.Consume(context1);
        await _consumer.Consume(context2);

        // Assert
        var rewards = await _context.UserRewards
            .Where(ur => ur.UserId == userId)
            .ToListAsync();

        rewards.Should().HaveCount(2, "Should have rewards for both days");
        
        // Verify that DailyGoalAchievedEvent was published TWICE (once per day)
        _publishEndpointMock.Verify(
            x => x.Publish(
                It.Is<DailyGoalAchievedEvent>(e => 
                    e.UserId == userId &&
                    e.Date == day1),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Goal event should be published for day 1");

        _publishEndpointMock.Verify(
            x => x.Publish(
                It.Is<DailyGoalAchievedEvent>(e => 
                    e.UserId == userId &&
                    e.Date == day2),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Goal event should be published for day 2");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

