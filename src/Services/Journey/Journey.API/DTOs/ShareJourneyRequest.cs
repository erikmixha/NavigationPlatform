namespace Journey.API.Controllers;

public sealed record ShareJourneyRequest
{
    public required List<string> SharedWithUserIds { get; init; }
}

