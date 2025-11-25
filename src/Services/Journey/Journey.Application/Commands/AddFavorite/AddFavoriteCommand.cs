using MediatR;
using Shared.Common.Result;

namespace Journey.Application.Commands.AddFavorite;

public sealed record AddFavoriteCommand(Guid JourneyId, string UserId) : IRequest<Result>;

