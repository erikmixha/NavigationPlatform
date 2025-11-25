using Journey.Application.Interfaces;
using Journey.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Common.Result;

namespace Journey.Application.Commands.UnshareJourney;

/// <summary>
/// Handler for unsharing a journey from a specific user.
/// </summary>
public sealed class UnshareJourneyCommandHandler : IRequestHandler<UnshareJourneyCommand, Result>
{
    private readonly IJourneyRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UnshareJourneyCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnshareJourneyCommandHandler"/> class.
    /// </summary>
    public UnshareJourneyCommandHandler(
        IJourneyRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<UnshareJourneyCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(UnshareJourneyCommand request, CancellationToken cancellationToken)
    {
        var journey = await _repository.GetByIdAsync(request.JourneyId, cancellationToken);
        if (journey is null)
        {
            return Result.Failure(new Error("Journey.NotFound", "Journey not found"));
        }

        if (journey.UserId != request.UserId)
        {
            return Result.Failure(new Error("Journey.Forbidden", "You can only unshare your own journeys"));
        }

        var share = await _repository.GetShareAsync(request.JourneyId, request.SharedWithUserId, cancellationToken);
        if (share is null)
        {
            return Result.Failure(new Error("Journey.ShareNotFound", "Journey share not found"));
        }

        await _repository.RemoveShareAsync(share, cancellationToken);

        var audit = new ShareAudit(request.JourneyId, "Unshared", request.UserId, request.SharedWithUserId);
        await _repository.AddShareAuditAsync(audit, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Journey {JourneyId} unshared by {UserId} from {SharedWithUserId}",
            request.JourneyId,
            request.UserId,
            request.SharedWithUserId);

        return Result.Success();
    }
}
