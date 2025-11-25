using Shared.Common.Result;

namespace Journey.Domain.ValueObjects;

public sealed record DistanceKm
{
    public decimal Value { get; }

    private DistanceKm(decimal value)
    {
        Value = value;
    }

    public static Result<DistanceKm> Create(decimal value)
    {
        if (value < 0)
        {
            return Result.Failure<DistanceKm>(new Error(
                "DistanceKm.Negative",
                "Distance cannot be negative"));
        }

        const decimal maxDistance = 2147483647m;
        if (value > maxDistance)
        {
            return Result.Failure<DistanceKm>(new Error(
                "DistanceKm.TooLarge",
                $"Distance cannot exceed {maxDistance:N0} km"));
        }

        return Result.Success(new DistanceKm(value));
    }

    public static implicit operator decimal(DistanceKm distance) => distance.Value;
}

