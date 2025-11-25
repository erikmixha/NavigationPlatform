using Journey.Application.Interfaces;
using Journey.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Common.Result;

namespace Journey.Application.Commands.AddFavorite;

/// <summary>
/// Handler for adding a journey to favorites.
/// </summary>
public sealed class AddFavoriteCommandHandler : IRequestHandler<AddFavoriteCommand, Result>
{
    private readonly IJourneyRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddFavoriteCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddFavoriteCommandHandler"/> class.
    /// </summary>
    public AddFavoriteCommandHandler(
        IJourneyRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<AddFavoriteCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(AddFavoriteCommand request, CancellationToken cancellationToken)
    {
        var journey = await _repository.GetByIdAsync(request.JourneyId, cancellationToken);
        if (journey is null)
        {
            return Result.Failure(new Error("Journey.NotFound", "Journey not found"));
        }

        if (!await _repository.CanAccessJourneyAsync(request.JourneyId, request.UserId, cancellationToken))
        {
            return Result.Failure(new Error("Journey.Forbidden", "You do not have access to this journey"));
        }

        var existingFavorite = await _repository.GetFavoriteAsync(request.JourneyId, request.UserId, cancellationToken);
        if (existingFavorite is not null)
        {
            _logger.LogInformation(
                "Journey {JourneyId} already in favorites for user {UserId} (idempotent operation)",
                request.JourneyId,
                request.UserId);
            return Result.Success();
        }

        var favorite = new JourneyFavorite(request.JourneyId, request.UserId);
        await _repository.AddFavoriteAsync(favorite, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Journey {JourneyId} added to favorites by user {UserId}",
            request.JourneyId,
            request.UserId);

        return Result.Success();
    }
}
