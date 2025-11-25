using Gateway.Application.DTOs;
using MediatR;
using Shared.Common.Result;

namespace Gateway.Application.Queries.GetUsersForSharing;

/// <summary>
/// Query to get all users (excluding current user) for sharing journeys.
/// </summary>
public sealed record GetUsersForSharingQuery : IRequest<Result<IEnumerable<UserInfoDto>>>
{
    /// <summary>
    /// Gets or sets the current user ID to exclude from results.
    /// </summary>
    public string CurrentUserId { get; init; } = string.Empty;
}

