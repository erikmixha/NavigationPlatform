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
public class JourneyUpdatedConsumerTests : IDisposable
{
    private readonly RewardDbContext _context;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly Mock<ILogger<JourneyUpdatedConsumer>> _loggerMock;
    private readonly JourneyUpdatedConsumer _consumer;
    private const decimal DailyGoalKm = 20.0m;
    private const int PointsPerKm = 10;

    public JourneyUpdatedConsumerTests()
    {
        var options = new DbContextOptionsBuilder<RewardDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new RewardDbContext(options);
        _publishEndpointMock = new Mock<IPublishEndpoint>();
        _loggerMock = new Mock<ILogger<JourneyUpdatedConsumer>>();
        
        var rewardSettings = Options.Create(new RewardSettings
        {
            DailyGoalKm = DailyGoalKm,
            PointsPerKm = PointsPerKm
        });
        
        _consumer = new JourneyUpdatedConsumer(
            _context,
            _publishEndpointMock.Object,
            _loggerMock.Object,
            rewardSettings);
    }

    [Fact]
    public async Task Consume_WhenSameDateAndDistanceIncreased_ShouldUpdateReward()
    {
        var userId = "test-user-1";
        var date = DateTime.UtcNow.Date;
        var journeyId = Guid.NewGuid();

        var existingReward = new UserReward(userId, date, 10.0m, 100);
        await _context.UserRewards.AddAsync(existingReward);
        await _context.SaveChangesAsync();

        var journeyUpdatedEvent = new JourneyUpdatedEvent
        {
            JourneyId = journeyId,
            UserId = userId,
            StartLocation = "Start",
            StartTime = new DateTime(date.Year, date.Month, date.Day, 10, 0, 0, DateTimeKind.Utc),
            ArrivalLocation = "End",
            ArrivalTime = new DateTime(date.Year, date.Month, date.Day, 11, 0, 0, DateTimeKind.Utc),
            TransportType = TransportType.Commercial.ToString(),
            DistanceKm = 15.0m,
            OldDistanceKm = 10.0m,
            OldStartTime = new DateTime(date.Year, date.Month, date.Day, 9, 0, 0, DateTimeKind.Utc),
            FavoritingUserIds = new List<string>()
        };

        var context = Mock.Of<ConsumeContext<JourneyUpdatedEvent>>(c => c.Message == journeyUpdatedEvent);

        await _consumer.Consume(context);

        var updatedReward = await _context.UserRewards
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.Date == date);

        updatedReward.Should().NotBeNull();
        updatedReward!.TotalDistanceKm.Should().Be(15.0m);
        updatedReward.Points.Should().Be(150);
    }

    [Fact]
    public async Task Consume_WhenSameDateAndDistanceDecreased_ShouldUpdateReward()
    {
        var userId = "test-user-2";
        var date = DateTime.UtcNow.Date;
        var journeyId = Guid.NewGuid();

        var existingReward = new UserReward(userId, date, 25.0m, 250);
        await _context.UserRewards.AddAsync(existingReward);
        await _context.SaveChangesAsync();

        var journeyUpdatedEvent = new JourneyUpdatedEvent
        {
            JourneyId = journeyId,
            UserId = userId,
            StartLocation = "Start",
            StartTime = new DateTime(date.Year, date.Month, date.Day, 10, 0, 0, DateTimeKind.Utc),
            ArrivalLocation = "End",
            ArrivalTime = new DateTime(date.Year, date.Month, date.Day, 11, 0, 0, DateTimeKind.Utc),
            TransportType = TransportType.Commercial.ToString(),
            DistanceKm = 20.0m,
            OldDistanceKm = 25.0m,
            OldStartTime = new DateTime(date.Year, date.Month, date.Day, 9, 0, 0, DateTimeKind.Utc),
            FavoritingUserIds = new List<string>()
        };

        var context = Mock.Of<ConsumeContext<JourneyUpdatedEvent>>(c => c.Message == journeyUpdatedEvent);

        await _consumer.Consume(context);

        var updatedReward = await _context.UserRewards
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.Date == date);

        updatedReward.Should().NotBeNull();
        updatedReward!.TotalDistanceKm.Should().Be(20.0m);
        updatedReward.Points.Should().Be(200);
    }

    [Fact]
    public async Task Consume_WhenDateChanged_ShouldMoveRewardToNewDate()
    {
        var userId = "test-user-3";
        var oldDate = DateTime.UtcNow.Date;
        var newDate = DateTime.UtcNow.Date.AddDays(1);
        var journeyId = Guid.NewGuid();

        var oldReward = new UserReward(userId, oldDate, 15.0m, 150);
        await _context.UserRewards.AddAsync(oldReward);
        await _context.SaveChangesAsync();

        var journeyUpdatedEvent = new JourneyUpdatedEvent
        {
            JourneyId = journeyId,
            UserId = userId,
            StartLocation = "Start",
            StartTime = new DateTime(newDate.Year, newDate.Month, newDate.Day, 10, 0, 0, DateTimeKind.Utc),
            ArrivalLocation = "End",
            ArrivalTime = new DateTime(newDate.Year, newDate.Month, newDate.Day, 11, 0, 0, DateTimeKind.Utc),
            TransportType = TransportType.Commercial.ToString(),
            DistanceKm = 15.0m,
            OldDistanceKm = 15.0m,
            OldStartTime = new DateTime(oldDate.Year, oldDate.Month, oldDate.Day, 9, 0, 0, DateTimeKind.Utc),
            FavoritingUserIds = new List<string>()
        };

        var context = Mock.Of<ConsumeContext<JourneyUpdatedEvent>>(c => c.Message == journeyUpdatedEvent);

        await _consumer.Consume(context);

        var oldDateReward = await _context.UserRewards
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.Date == oldDate);
        var newDateReward = await _context.UserRewards
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.Date == newDate);

        oldDateReward.Should().NotBeNull();
        oldDateReward!.TotalDistanceKm.Should().Be(0m);
        newDateReward.Should().NotBeNull();
        newDateReward!.TotalDistanceKm.Should().Be(15.0m);
    }

    [Fact]
    public async Task Consume_WhenCrossingGoalThreshold_ShouldPublishDailyGoalAchievedEvent()
    {
        var userId = "test-user-4";
        var date = DateTime.UtcNow.Date;
        var journeyId = Guid.NewGuid();

        var existingReward = new UserReward(userId, date, 15.0m, 150);
        await _context.UserRewards.AddAsync(existingReward);
        await _context.SaveChangesAsync();

        var journeyUpdatedEvent = new JourneyUpdatedEvent
        {
            JourneyId = journeyId,
            UserId = userId,
            StartLocation = "Start",
            StartTime = new DateTime(date.Year, date.Month, date.Day, 10, 0, 0, DateTimeKind.Utc),
            ArrivalLocation = "End",
            ArrivalTime = new DateTime(date.Year, date.Month, date.Day, 11, 0, 0, DateTimeKind.Utc),
            TransportType = TransportType.Commercial.ToString(),
            DistanceKm = 10.0m,
            OldDistanceKm = 5.0m,
            OldStartTime = new DateTime(date.Year, date.Month, date.Day, 9, 0, 0, DateTimeKind.Utc),
            FavoritingUserIds = new List<string>()
        };

        var context = Mock.Of<ConsumeContext<JourneyUpdatedEvent>>(c => c.Message == journeyUpdatedEvent);

        await _consumer.Consume(context);

        _publishEndpointMock.Verify(
            x => x.Publish(
                It.Is<DailyGoalAchievedEvent>(e => 
                    e.UserId == userId &&
                    e.Date == date &&
                    e.TotalDistanceKm == 20.0m &&
                    e.GoalDistanceKm == DailyGoalKm),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Consume_WhenNoDistanceChange_ShouldNotUpdateReward()
    {
        var userId = "test-user-5";
        var date = DateTime.UtcNow.Date;
        var journeyId = Guid.NewGuid();

        var existingReward = new UserReward(userId, date, 10.0m, 100);
        await _context.UserRewards.AddAsync(existingReward);
        await _context.SaveChangesAsync();

        var journeyUpdatedEvent = new JourneyUpdatedEvent
        {
            JourneyId = journeyId,
            UserId = userId,
            StartLocation = "Start",
            StartTime = new DateTime(date.Year, date.Month, date.Day, 10, 0, 0, DateTimeKind.Utc),
            ArrivalLocation = "End",
            ArrivalTime = new DateTime(date.Year, date.Month, date.Day, 11, 0, 0, DateTimeKind.Utc),
            TransportType = TransportType.Commercial.ToString(),
            DistanceKm = 10.0m,
            OldDistanceKm = 10.0m,
            OldStartTime = new DateTime(date.Year, date.Month, date.Day, 9, 0, 0, DateTimeKind.Utc),
            FavoritingUserIds = new List<string>()
        };

        var context = Mock.Of<ConsumeContext<JourneyUpdatedEvent>>(c => c.Message == journeyUpdatedEvent);

        await _consumer.Consume(context);

        var reward = await _context.UserRewards
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.Date == date);

        reward.Should().NotBeNull();
        reward!.TotalDistanceKm.Should().Be(10.0m);
        reward.Points.Should().Be(100);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

