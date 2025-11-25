using MediatR;
using Shared.Common.Result;

namespace Journey.Application.Commands.RevokePublicLink;

public sealed record RevokePublicLinkCommand(Guid JourneyId, string UserId) : IRequest<Result>;

