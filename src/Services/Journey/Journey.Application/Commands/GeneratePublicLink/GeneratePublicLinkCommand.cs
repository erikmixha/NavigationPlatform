using MediatR;
using Shared.Common.Result;

namespace Journey.Application.Commands.GeneratePublicLink;

public sealed record GeneratePublicLinkCommand(Guid JourneyId, string UserId) : IRequest<Result<string>>;

