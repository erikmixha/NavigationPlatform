using Journey.Application.Interfaces;
using Journey.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Common.Result;

namespace Journey.Application.Commands.RevokePublicLink;

/// <summary>
/// Handler for revoking a public link for a journey.
/// </summary>
public sealed class RevokePublicLinkCommandHandler : IRequestHandler<RevokePublicLinkCommand, Result>
{
    private readonly IJourneyRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RevokePublicLinkCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RevokePublicLinkCommandHandler"/> class.
    /// </summary>
    public RevokePublicLinkCommandHandler(
        IJourneyRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<RevokePublicLinkCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(RevokePublicLinkCommand request, CancellationToken cancellationToken)
    {
        var journey = await _repository.GetByIdAsync(request.JourneyId, cancellationToken);
        if (journey is null)
        {
            return Result.Failure(new Error("Journey.NotFound", "Journey not found"));
        }

        if (journey.UserId != request.UserId)
        {
            return Result.Failure(new Error("Journey.Forbidden", "You can only revoke public links for your own journeys"));
        }

        var publicLink = await _repository.GetActivePublicLinkByJourneyIdAsync(request.JourneyId, cancellationToken);
        if (publicLink is null)
        {
            return Result.Failure(new Error("Journey.PublicLinkNotFound", "Active public link not found"));
        }

        publicLink.Revoke();
        _repository.UpdatePublicLink(publicLink);

        var audit = new ShareAudit(request.JourneyId, "PublicLinkRevoked", request.UserId);
        await _repository.AddShareAuditAsync(audit, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Public link revoked for journey {JourneyId} by user {UserId}",
            request.JourneyId,
            request.UserId);

        return Result.Success();
    }
}
