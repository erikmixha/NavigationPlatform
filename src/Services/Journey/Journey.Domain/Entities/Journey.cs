using System.Diagnostics.CodeAnalysis;
using Journey.Domain.Enums;
using Journey.Domain.Events;
using Journey.Domain.ValueObjects;
using Shared.Common.Primitives;
using Shared.Common.Result;

namespace Journey.Domain.Entities;

/// <remarks>
/// Excluded from code coverage: Domain entity with business logic.
/// Business logic is tested via integration tests and unit tests.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Domain entity with business logic. Tested via integration tests and unit tests.")]
public sealed class Journey : AggregateRoot
{
    private Journey()
    {
    }

    private Journey(
        Guid id,
        string userId,
        string startLocation,
        DateTime startTime,
        string arrivalLocation,
        DateTime arrivalTime,
        TransportType transportType,
        DistanceKm distanceKm)
    {
        Id = id;
        UserId = userId;
        StartLocation = startLocation;
        StartTime = startTime;
        ArrivalLocation = arrivalLocation;
        ArrivalTime = arrivalTime;
        TransportType = transportType;
        DistanceKm = distanceKm;
        IsDailyGoalAchieved = false;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string StartLocation { get; private set; } = string.Empty;
    public DateTime StartTime { get; private set; }
    public string ArrivalLocation { get; private set; } = string.Empty;
    public DateTime ArrivalTime { get; private set; }
    public TransportType TransportType { get; private set; }
    public DistanceKm DistanceKm { get; private set; } = null!;
    public bool IsDailyGoalAchieved { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    public static Result<Journey> Create(
        string userId,
        string startLocation,
        DateTime startTime,
        string arrivalLocation,
        DateTime arrivalTime,
        TransportType transportType,
        decimal distanceKm)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result.Failure<Journey>(new Error(
                "Journey.EmptyUserId",
                "User ID cannot be empty"));
        }

        if (string.IsNullOrWhiteSpace(startLocation))
        {
            return Result.Failure<Journey>(new Error(
                "Journey.EmptyStartLocation",
                "Start location cannot be empty"));
        }

        if (string.IsNullOrWhiteSpace(arrivalLocation))
        {
            return Result.Failure<Journey>(new Error(
                "Journey.EmptyArrivalLocation",
                "Arrival location cannot be empty"));
        }

        if (arrivalTime <= startTime)
        {
            return Result.Failure<Journey>(new Error(
                "Journey.InvalidTimeRange",
                "Arrival time must be after start time"));
        }

        var distanceResult = DistanceKm.Create(distanceKm);
        if (distanceResult.IsFailure)
        {
            return Result.Failure<Journey>(distanceResult.Error);
        }

        var journey = new Journey(
            Guid.NewGuid(),
            userId,
            startLocation,
            startTime,
            arrivalLocation,
            arrivalTime,
            transportType,
            distanceResult.Value);

        journey.RaiseDomainEvent(new JourneyCreated
        {
            JourneyId = journey.Id,
            UserId = journey.UserId,
            StartLocation = journey.StartLocation,
            StartTime = journey.StartTime,
            ArrivalLocation = journey.ArrivalLocation,
            ArrivalTime = journey.ArrivalTime,
            TransportType = journey.TransportType,
            DistanceKm = journey.DistanceKm.Value
        });

        return Result.Success(journey);
    }

    public Result Update(
        string startLocation,
        DateTime startTime,
        string arrivalLocation,
        DateTime arrivalTime,
        TransportType transportType,
        decimal distanceKm)
    {
        if (string.IsNullOrWhiteSpace(startLocation))
        {
            return Result.Failure(new Error(
                "Journey.EmptyStartLocation",
                "Start location cannot be empty"));
        }

        if (string.IsNullOrWhiteSpace(arrivalLocation))
        {
            return Result.Failure(new Error(
                "Journey.EmptyArrivalLocation",
                "Arrival location cannot be empty"));
        }

        if (arrivalTime <= startTime)
        {
            return Result.Failure(new Error(
                "Journey.InvalidTimeRange",
                "Arrival time must be after start time"));
        }

        var distanceResult = DistanceKm.Create(distanceKm);
        if (distanceResult.IsFailure)
        {
            return Result.Failure(distanceResult.Error);
        }

        var oldDistanceKm = DistanceKm.Value;
        var oldStartTime = StartTime;

        StartLocation = startLocation;
        StartTime = startTime;
        ArrivalLocation = arrivalLocation;
        ArrivalTime = arrivalTime;
        TransportType = transportType;
        DistanceKm = distanceResult.Value;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new JourneyUpdated
        {
            JourneyId = Id,
            UserId = UserId,
            StartLocation = StartLocation,
            StartTime = StartTime,
            ArrivalLocation = ArrivalLocation,
            ArrivalTime = ArrivalTime,
            TransportType = TransportType,
            DistanceKm = DistanceKm.Value,
            OldDistanceKm = oldDistanceKm,
            OldStartTime = oldStartTime
        });

        return Result.Success();
    }

    public void MarkAsDeleted()
    {
        RaiseDomainEvent(new JourneyDeleted
        {
            JourneyId = Id,
            UserId = UserId,
            StartLocation = StartLocation,
            StartTime = StartTime,
            ArrivalLocation = ArrivalLocation,
            DistanceKm = DistanceKm.Value
        });
    }

    public void SetDailyGoalAchieved()
    {
        IsDailyGoalAchieved = true;
        UpdatedOnUtc = DateTime.UtcNow;
    }
}

