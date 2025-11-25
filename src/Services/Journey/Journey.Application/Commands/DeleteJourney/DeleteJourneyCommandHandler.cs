using Journey.Application.Interfaces;
using MediatR;
using Shared.Common.Result;

namespace Journey.Application.Commands.DeleteJourney;

/// <summary>
/// Handler for deleting a journey.
/// </summary>
public sealed class DeleteJourneyCommandHandler : IRequestHandler<DeleteJourneyCommand, Result>
{
    private readonly IJourneyRepository _journeyRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteJourneyCommandHandler"/> class.
    /// </summary>
    public DeleteJourneyCommandHandler(IJourneyRepository journeyRepository, IUnitOfWork unitOfWork)
    {
        _journeyRepository = journeyRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteJourneyCommand request, CancellationToken cancellationToken)
    {
        var journey = await _journeyRepository.GetByIdAsync(request.Id, cancellationToken);

        if (journey is null)
        {
            return Result.Failure(new Error("Journey.NotFound", "Journey not found"));
        }

        if (journey.UserId != request.UserId)
        {
            return Result.Failure(new Error("Journey.Forbidden", "You are not authorized to delete this journey"));
        }

        journey.MarkAsDeleted();
        _journeyRepository.Remove(journey);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
