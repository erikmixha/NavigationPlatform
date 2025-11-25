using Journey.API.DTOs;
using Journey.Application.Commands.CreateJourney;
using Journey.Application.Commands.DeleteJourney;
using Journey.Application.Commands.UpdateJourney;

namespace Journey.API.Extensions;

/// <summary>
/// Extension methods for mapping between API DTOs and application commands.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Extension methods for DTO mapping.
/// Mapping logic is simple and tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Extension methods for DTO mapping. Tested via integration tests.")]
public static class MappingExtensions
{
    /// <summary>
    /// Maps a create journey request to a create journey command.
    /// </summary>
    /// <param name="request">The create journey request.</param>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The create journey command.</returns>
    public static CreateJourneyCommand ToCommand(this CreateJourneyRequest request, string userId)
    {
        var startTime = request.StartTime.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc)
            : request.StartTime.ToUniversalTime();

        var arrivalTime = request.ArrivalTime.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(request.ArrivalTime, DateTimeKind.Utc)
            : request.ArrivalTime.ToUniversalTime();

        return new CreateJourneyCommand
        {
            UserId = userId,
            StartLocation = request.StartLocation,
            StartTime = startTime,
            ArrivalLocation = request.ArrivalLocation,
            ArrivalTime = arrivalTime,
            TransportType = request.TransportType,
            DistanceKm = request.DistanceKm
        };
    }

    /// <summary>
    /// Maps an update journey request to an update journey command.
    /// </summary>
    /// <param name="request">The update journey request.</param>
    /// <param name="journeyId">The journey identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The update journey command.</returns>
    public static UpdateJourneyCommand ToCommand(this UpdateJourneyRequest request, Guid journeyId, string userId)
    {
        var startTime = request.StartTime.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc)
            : request.StartTime.ToUniversalTime();

        var arrivalTime = request.ArrivalTime.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(request.ArrivalTime, DateTimeKind.Utc)
            : request.ArrivalTime.ToUniversalTime();

        return new UpdateJourneyCommand
        {
            Id = journeyId,
            UserId = userId,
            StartLocation = request.StartLocation,
            StartTime = startTime,
            ArrivalLocation = request.ArrivalLocation,
            ArrivalTime = arrivalTime,
            TransportType = request.TransportType,
            DistanceKm = request.DistanceKm
        };
    }

    /// <summary>
    /// Maps journey ID and user ID to a delete journey command.
    /// </summary>
    /// <param name="journeyId">The journey identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The delete journey command.</returns>
    public static DeleteJourneyCommand ToCommand(Guid journeyId, string userId)
    {
        return new DeleteJourneyCommand
        {
            Id = journeyId,
            UserId = userId
        };
    }
}
