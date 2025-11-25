using MediatR;
using Shared.Common.Result;

namespace Journey.Application.Commands.RemoveFavorite;

public sealed record RemoveFavoriteCommand(Guid JourneyId, string UserId) : IRequest<Result>;

