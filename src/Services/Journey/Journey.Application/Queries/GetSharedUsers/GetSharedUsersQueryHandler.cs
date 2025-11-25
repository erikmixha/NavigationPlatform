using Journey.Application.Interfaces;
using MediatR;
using Shared.Common.Result;

namespace Journey.Application.Queries.GetSharedUsers;

/// <summary>
/// Handler for getting users a journey is shared with.
/// </summary>
public sealed class GetSharedUsersQueryHandler : IRequestHandler<GetSharedUsersQuery, Result<List<string>>>
{
    private readonly IJourneyRepository _journeyRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSharedUsersQueryHandler"/> class.
    /// </summary>
    public GetSharedUsersQueryHandler(IJourneyRepository journeyRepository)
    {
        _journeyRepository = journeyRepository;
    }

    /// <inheritdoc />
    public async Task<Result<List<string>>> Handle(GetSharedUsersQuery request, CancellationToken cancellationToken)
    {
        var journey = await _journeyRepository.GetByIdAsync(request.JourneyId, cancellationToken);
        if (journey is null)
        {
            return Result.Failure<List<string>>(new Error("Journey.NotFound", "Journey not found"));
        }

        if (journey.UserId != request.UserId)
        {
            return Result.Failure<List<string>>(new Error("Journey.Forbidden", "You are not authorized to view sharing information for this journey"));
        }

        var sharedWithUserIds = await _journeyRepository.GetSharedWithUserIdsAsync(request.JourneyId, cancellationToken);
        return Result.Success(sharedWithUserIds);
    }
}
