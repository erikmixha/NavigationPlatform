using Journey.Application.Interfaces;
using Journey.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Common.Result;

namespace Journey.Application.Commands.GeneratePublicLink;

/// <summary>
/// Handler for generating a public link for a journey.
/// </summary>
public sealed class GeneratePublicLinkCommandHandler : IRequestHandler<GeneratePublicLinkCommand, Result<string>>
{
    private readonly IJourneyRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GeneratePublicLinkCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneratePublicLinkCommandHandler"/> class.
    /// </summary>
    public GeneratePublicLinkCommandHandler(
        IJourneyRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<GeneratePublicLinkCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<string>> Handle(GeneratePublicLinkCommand request, CancellationToken cancellationToken)
    {
        var journey = await _repository.GetByIdAsync(request.JourneyId, cancellationToken);
        if (journey is null)
        {
            return Result.Failure<string>(new Error("Journey.NotFound", "Journey not found"));
        }

        if (journey.UserId != request.UserId)
        {
            return Result.Failure<string>(new Error("Journey.Forbidden", "You can only generate public links for your own journeys"));
        }

        var existingLink = await _repository.GetActivePublicLinkByJourneyIdAsync(request.JourneyId, cancellationToken);
        if (existingLink is not null)
        {
            return Result.Success(existingLink.Token);
        }

        var linkResult = PublicLink.Create(request.JourneyId, request.UserId);
        if (linkResult.IsFailure)
        {
            return Result.Failure<string>(linkResult.Error);
        }

        await _repository.AddPublicLinkAsync(linkResult.Value, cancellationToken);

        var audit = new ShareAudit(request.JourneyId, "PublicLinkGenerated", request.UserId);
        await _repository.AddShareAuditAsync(audit, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Public link generated for journey {JourneyId} by user {UserId}",
            request.JourneyId,
            request.UserId);

        return Result.Success(linkResult.Value.Token);
    }
}
