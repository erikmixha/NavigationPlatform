using Journey.Application.Interfaces;
using Journey.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Common.Result;

namespace Journey.Application.Commands.ShareJourney;

/// <summary>
/// Handler for sharing a journey with one or more users.
/// </summary>
public sealed class ShareJourneyCommandHandler : IRequestHandler<ShareJourneyCommand, Result>
{
    private readonly IJourneyRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ShareJourneyCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShareJourneyCommandHandler"/> class.
    /// </summary>
    public ShareJourneyCommandHandler(
        IJourneyRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<ShareJourneyCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(ShareJourneyCommand request, CancellationToken cancellationToken)
    {
        var journey = await _repository.GetByIdAsync(request.JourneyId, cancellationToken);
        if (journey is null)
        {
            return Result.Failure(new Error("Journey.NotFound", "Journey not found"));
        }

        if (journey.UserId != request.SharedByUserId)
        {
            return Result.Failure(new Error("Journey.Forbidden", "You can only share your own journeys"));
        }

        if (request.SharedWithUserIds == null || !request.SharedWithUserIds.Any())
        {
            return Result.Failure(new Error("Journey.InvalidShare", "At least one user ID is required"));
        }

        var distinctUserIds = request.SharedWithUserIds.Distinct().ToList();

        foreach (var sharedWithUserId in distinctUserIds)
        {
            if (journey.UserId == sharedWithUserId)
            {
                _logger.LogWarning(
                    "Attempt to share journey {JourneyId} with self by {SharedByUserId}",
                    request.JourneyId,
                    request.SharedByUserId);
                continue;
            }

            var existingShare = await _repository.GetShareAsync(request.JourneyId, sharedWithUserId, cancellationToken);
            if (existingShare is not null)
            {
                _logger.LogInformation(
                    "Journey {JourneyId} already shared with {SharedWithUserId}, skipping",
                    request.JourneyId,
                    sharedWithUserId);
                continue;
            }

            var share = new JourneyShare(request.JourneyId, sharedWithUserId, request.SharedByUserId);
            await _repository.AddShareAsync(share, cancellationToken);

            var audit = new ShareAudit(request.JourneyId, "Shared", request.SharedByUserId, sharedWithUserId);
            await _repository.AddShareAuditAsync(audit, cancellationToken);

            _logger.LogInformation(
                "Journey {JourneyId} shared by {SharedByUserId} with {SharedWithUserId}",
                request.JourneyId,
                request.SharedByUserId,
                sharedWithUserId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
