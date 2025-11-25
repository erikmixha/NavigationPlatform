using MediatR;
using Shared.Common.Result;

namespace Journey.Application.Commands.UnshareJourney;

public sealed record UnshareJourneyCommand(
    Guid JourneyId,
    string SharedWithUserId,
    string UserId) : IRequest<Result>;

