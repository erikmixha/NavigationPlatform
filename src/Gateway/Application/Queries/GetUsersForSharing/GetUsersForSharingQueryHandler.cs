using Gateway.Application.DTOs;
using Gateway.Application.Interfaces;
using MediatR;
using Shared.Common.Result;

namespace Gateway.Application.Queries.GetUsersForSharing;

/// <summary>
/// Handler for getting users available for sharing journeys.
/// </summary>
public sealed class GetUsersForSharingQueryHandler : IRequestHandler<GetUsersForSharingQuery, Result<IEnumerable<UserInfoDto>>>
{
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly ILogger<GetUsersForSharingQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetUsersForSharingQueryHandler"/> class.
    /// </summary>
    public GetUsersForSharingQueryHandler(
        IKeycloakUserService keycloakUserService,
        ILogger<GetUsersForSharingQueryHandler> logger)
    {
        _keycloakUserService = keycloakUserService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<UserInfoDto>>> Handle(GetUsersForSharingQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(request.CurrentUserId))
            {
                return Result.Failure<IEnumerable<UserInfoDto>>(
                    new Shared.Common.Result.Error("User.Invalid", "Current user ID is required"));
            }

            var users = await _keycloakUserService.GetUsersForSharingAsync(request.CurrentUserId, cancellationToken);
            return Result.Success(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users from Keycloak");
            return Result.Failure<IEnumerable<UserInfoDto>>(
                new Shared.Common.Result.Error("User.RetrievalFailed", "An error occurred while retrieving users"));
        }
    }
}

