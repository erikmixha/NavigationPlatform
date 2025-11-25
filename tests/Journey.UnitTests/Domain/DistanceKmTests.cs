using FluentAssertions;
using Journey.Domain.ValueObjects;
using Xunit;

namespace Journey.UnitTests.Domain;

public sealed class DistanceKmTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(10.5)]
    [InlineData(999.99)]
    public void Create_WithValidDistance_ShouldSucceed(decimal distance)
    {
        var result = DistanceKm.Create(distance);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(distance);
    }

    [Fact]
    public void Create_WithNegativeDistance_ShouldFail()
    {
        var result = DistanceKm.Create(-1m);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DistanceKm.Negative");
    }

    [Fact]
    public void Create_WithDistanceTooLarge_ShouldFail()
    {
        var result = DistanceKm.Create(2147483648m); // Max is 2147483647m

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DistanceKm.TooLarge");
    }
}

