using MediatR;
using Shared.Common.Result;

namespace Journey.Application.Commands.DeleteJourney;

public sealed record DeleteJourneyCommand : IRequest<Result>
{
    public Guid Id { get; init; }
    public string UserId { get; init; } = string.Empty;
}

