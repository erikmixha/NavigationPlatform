namespace Journey.API.Controllers;

public sealed record PublicLinkResponse
{
    public required string Token { get; init; }
    public required string Url { get; init; }
}

