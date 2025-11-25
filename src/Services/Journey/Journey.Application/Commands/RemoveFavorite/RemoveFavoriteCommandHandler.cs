using Journey.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Common.Result;

namespace Journey.Application.Commands.RemoveFavorite;

/// <summary>
/// Handler for removing a journey from favorites.
/// </summary>
public sealed class RemoveFavoriteCommandHandler : IRequestHandler<RemoveFavoriteCommand, Result>
{
    private readonly IJourneyRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RemoveFavoriteCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveFavoriteCommandHandler"/> class.
    /// </summary>
    public RemoveFavoriteCommandHandler(
        IJourneyRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<RemoveFavoriteCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(RemoveFavoriteCommand request, CancellationToken cancellationToken)
    {
        var favorite = await _repository.GetFavoriteAsync(request.JourneyId, request.UserId, cancellationToken);
        if (favorite is null)
        {
            return Result.Failure(new Error("Journey.FavoriteNotFound", "Favorite not found"));
        }

        await _repository.RemoveFavoriteAsync(favorite, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Journey {JourneyId} removed from favorites by user {UserId}",
            request.JourneyId,
            request.UserId);

        return Result.Success();
    }
}
