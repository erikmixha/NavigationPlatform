using FluentAssertions;
using Journey.Domain.Enums;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Reward.Domain.Entities;
using Reward.Infrastructure.Persistence;
using Reward.Worker.Consumers;
using Shared.Common.Configuration;
using Shared.Messaging.Events;
using Xunit;

namespace Reward.UnitTests.Consumers;

[Trait("Category", "Unit")]
public class JourneyDeletedConsumerTests : IDisposable
{
    private readonly RewardDbContext _context;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly Mock<ILogger<JourneyDeletedConsumer>> _loggerMock;
    private readonly JourneyDeletedConsumer _consumer;
    private const int PointsPerKm = 10;

    public JourneyDeletedConsumerTests()
    {
        var options = new DbContextOptionsBuilder<RewardDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new RewardDbContext(options);
        _publishEndpointMock = new Mock<IPublishEndpoint>();
        _loggerMock = new Mock<ILogger<JourneyDeletedConsumer>>();
        
        var rewardSettings = Options.Create(new RewardSettings
        {
            DailyGoalKm = 20.0m,
            PointsPerKm = PointsPerKm
        });
        
        _consumer = new JourneyDeletedConsumer(
            _context,
            _publishEndpointMock.Object,
            _loggerMock.Object,
            rewardSettings);
    }

    [Fact]
    public async Task Consume_ShouldSubtractDistanceAndPoints_WhenRewardExists()
    {
        var userId = "test-user-1";
        var date = DateTime.UtcNow.Date;
        var journeyId = Guid.NewGuid();

        var existingReward = new UserReward(userId, date, 25.0m, 250);
        await _context.UserRewards.AddAsync(existingReward);
        await _context.SaveChangesAsync();

        var journeyDeletedEvent = new JourneyDeletedEvent
        {
            JourneyId = journeyId,
            UserId = userId,
            StartLocation = "Start",
            StartTime = new DateTime(date.Year, date.Month, date.Day, 10, 0, 0, DateTimeKind.Utc),
            ArrivalLocation = "End",
            DistanceKm = 10.0m,
            FavoritingUserIds = new List<string>()
        };

        var context = Mock.Of<ConsumeContext<JourneyDeletedEvent>>(c => c.Message == journeyDeletedEvent);

        await _consumer.Consume(context);

        var updatedReward = await _context.UserRewards
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.Date == date);

        updatedReward.Should().NotBeNull();
        updatedReward!.TotalDistanceKm.Should().Be(15.0m);
        updatedReward.Points.Should().Be(150);
    }

    [Fact]
    public async Task Consume_ShouldNotThrow_WhenRewardDoesNotExist()
    {
        var userId = "test-user-2";
        var date = DateTime.UtcNow.Date;
        var journeyId = Guid.NewGuid();

        var journeyDeletedEvent = new JourneyDeletedEvent
        {
            JourneyId = journeyId,
            UserId = userId,
            StartLocation = "Start",
            StartTime = new DateTime(date.Year, date.Month, date.Day, 10, 0, 0, DateTimeKind.Utc),
            ArrivalLocation = "End",
            DistanceKm = 10.0m,
            FavoritingUserIds = new List<string>()
        };

        var context = Mock.Of<ConsumeContext<JourneyDeletedEvent>>(c => c.Message == journeyDeletedEvent);

        await _consumer.Invoking(c => c.Consume(context))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task Consume_ShouldHandleZeroDistance_WhenSubtracting()
    {
        var userId = "test-user-3";
        var date = DateTime.UtcNow.Date;
        var journeyId = Guid.NewGuid();

        var existingReward = new UserReward(userId, date, 10.0m, 100);
        await _context.UserRewards.AddAsync(existingReward);
        await _context.SaveChangesAsync();

        var journeyDeletedEvent = new JourneyDeletedEvent
        {
            JourneyId = journeyId,
            UserId = userId,
            StartLocation = "Start",
            StartTime = new DateTime(date.Year, date.Month, date.Day, 10, 0, 0, DateTimeKind.Utc),
            ArrivalLocation = "End",
            DistanceKm = 10.0m,
            FavoritingUserIds = new List<string>()
        };

        var context = Mock.Of<ConsumeContext<JourneyDeletedEvent>>(c => c.Message == journeyDeletedEvent);

        await _consumer.Consume(context);

        var updatedReward = await _context.UserRewards
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.Date == date);

        updatedReward.Should().NotBeNull();
        updatedReward!.TotalDistanceKm.Should().Be(0m);
        updatedReward.Points.Should().Be(0);
    }

    [Fact]
    public async Task Consume_ShouldHandleMultipleDeletions()
    {
        var userId = "test-user-4";
        var date = DateTime.UtcNow.Date;

        var existingReward = new UserReward(userId, date, 30.0m, 300);
        await _context.UserRewards.AddAsync(existingReward);
        await _context.SaveChangesAsync();

        var journey1DeletedEvent = new JourneyDeletedEvent
        {
            JourneyId = Guid.NewGuid(),
            UserId = userId,
            StartLocation = "Start1",
            StartTime = new DateTime(date.Year, date.Month, date.Day, 10, 0, 0, DateTimeKind.Utc),
            ArrivalLocation = "End1",
            DistanceKm = 10.0m,
            FavoritingUserIds = new List<string>()
        };

        var journey2DeletedEvent = new JourneyDeletedEvent
        {
            JourneyId = Guid.NewGuid(),
            UserId = userId,
            StartLocation = "Start2",
            StartTime = new DateTime(date.Year, date.Month, date.Day, 11, 0, 0, DateTimeKind.Utc),
            ArrivalLocation = "End2",
            DistanceKm = 5.0m,
            FavoritingUserIds = new List<string>()
        };

        var context1 = Mock.Of<ConsumeContext<JourneyDeletedEvent>>(c => c.Message == journey1DeletedEvent);
        var context2 = Mock.Of<ConsumeContext<JourneyDeletedEvent>>(c => c.Message == journey2DeletedEvent);

        await _consumer.Consume(context1);
        await _consumer.Consume(context2);

        var updatedReward = await _context.UserRewards
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.Date == date);

        updatedReward.Should().NotBeNull();
        updatedReward!.TotalDistanceKm.Should().Be(15.0m);
        updatedReward.Points.Should().Be(150);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

