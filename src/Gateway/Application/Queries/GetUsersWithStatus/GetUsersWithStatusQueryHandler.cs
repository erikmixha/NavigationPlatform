using Gateway.Application.DTOs;
using Gateway.Application.Interfaces;
using MediatR;
using Shared.Common.Result;

namespace Gateway.Application.Queries.GetUsersWithStatus;

/// <summary>
/// Handler for getting all users with their account status.
/// </summary>
public sealed class GetUsersWithStatusQueryHandler : IRequestHandler<GetUsersWithStatusQuery, Result<IEnumerable<UserWithStatusDto>>>
{
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly ILogger<GetUsersWithStatusQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetUsersWithStatusQueryHandler"/> class.
    /// </summary>
    public GetUsersWithStatusQueryHandler(
        IKeycloakUserService keycloakUserService,
        ILogger<GetUsersWithStatusQueryHandler> logger)
    {
        _keycloakUserService = keycloakUserService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<UserWithStatusDto>>> Handle(GetUsersWithStatusQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var users = await _keycloakUserService.GetUsersWithStatusAsync(cancellationToken);
            return Result.Success(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users with status");
            return Result.Failure<IEnumerable<UserWithStatusDto>>(
                new Shared.Common.Result.Error("User.RetrievalFailed", "An error occurred while retrieving users"));
        }
    }
}

