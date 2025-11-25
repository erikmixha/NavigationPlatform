using Gateway.Application.DTOs;
using MediatR;
using Shared.Common.Result;

namespace Gateway.Application.Queries.GetCurrentUser;

/// <summary>
/// Query to get the current authenticated user information.
/// </summary>
public sealed record GetCurrentUserQuery : IRequest<Result<UserDto>>
{
    /// <summary>
    /// Gets or sets the claims principal representing the user.
    /// </summary>
    public System.Security.Claims.ClaimsPrincipal User { get; init; } = null!;
}

