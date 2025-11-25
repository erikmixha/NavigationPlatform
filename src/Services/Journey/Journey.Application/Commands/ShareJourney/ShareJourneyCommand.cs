using MediatR;
using Shared.Common.Result;

namespace Journey.Application.Commands.ShareJourney;

public sealed record ShareJourneyCommand(
    Guid JourneyId,
    List<string> SharedWithUserIds,
    string SharedByUserId) : IRequest<Result>;

