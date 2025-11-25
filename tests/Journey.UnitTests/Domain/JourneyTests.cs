using FluentAssertions;
using Journey.Domain.Enums;
using Xunit;

namespace Journey.UnitTests.Domain;

public sealed class JourneyTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var result = Journey.Domain.Entities.Journey.Create(
            userId: "user-123",
            startLocation: "Home",
            startTime: DateTime.UtcNow,
            arrivalLocation: "Work",
            arrivalTime: DateTime.UtcNow.AddHours(1),
            transportType: TransportType.Commercial,
            distanceKm: 15.50m);

        result.IsSuccess.Should().BeTrue();
        result.Value.DistanceKm.Value.Should().Be(15.50m);
    }

    [Fact]
    public void Create_WithNegativeDistance_ShouldFail()
    {
        var result = Journey.Domain.Entities.Journey.Create(
            userId: "user-123",
            startLocation: "Home",
            startTime: DateTime.UtcNow,
            arrivalLocation: "Work",
            arrivalTime: DateTime.UtcNow.AddHours(1),
            transportType: TransportType.Commercial,
            distanceKm: -5m);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DistanceKm.Negative");
    }

    [Fact]
    public void Create_WithArrivalBeforeStart_ShouldFail()
    {
        var startTime = DateTime.UtcNow;
        var arrivalTime = startTime.AddHours(-1);

        var result = Journey.Domain.Entities.Journey.Create(
            userId: "user-123",
            startLocation: "Home",
            startTime: startTime,
            arrivalLocation: "Work",
            arrivalTime: arrivalTime,
            transportType: TransportType.Commercial,
            distanceKm: 15.50m);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Journey.InvalidTimeRange");
    }

    [Fact]
    public void Update_WithValidData_ShouldRaiseJourneyUpdatedEvent()
    {
        var journey = Journey.Domain.Entities.Journey.Create(
            userId: "user-123",
            startLocation: "Home",
            startTime: DateTime.UtcNow,
            arrivalLocation: "Work",
            arrivalTime: DateTime.UtcNow.AddHours(1),
            transportType: TransportType.Commercial,
            distanceKm: 15.50m).Value;

        journey.ClearDomainEvents();

        var updateResult = journey.Update(
            startLocation: "Office",
            startTime: DateTime.UtcNow,
            arrivalLocation: "Home",
            arrivalTime: DateTime.UtcNow.AddHours(2),
            transportType: TransportType.Cargo,
            distanceKm: 10.00m);

        updateResult.IsSuccess.Should().BeTrue();
        journey.DomainEvents.Should().HaveCount(1);
        journey.DomainEvents.First().Should().BeOfType<Journey.Domain.Events.JourneyUpdated>();
    }

    [Fact]
    public void SetDailyGoalAchieved_ShouldSetFlagToTrue()
    {
        var journey = Journey.Domain.Entities.Journey.Create(
            userId: "user-123",
            startLocation: "Home",
            startTime: DateTime.UtcNow,
            arrivalLocation: "Work",
            arrivalTime: DateTime.UtcNow.AddHours(1),
            transportType: TransportType.Commercial,
            distanceKm: 20.00m).Value;

        journey.IsDailyGoalAchieved.Should().BeFalse();

        journey.SetDailyGoalAchieved();

        journey.IsDailyGoalAchieved.Should().BeTrue();
    }
}

