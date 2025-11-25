using Gateway.Application.DTOs;
using MediatR;
using Shared.Common.Result;

namespace Gateway.Application.Queries.GetCurrentUser;

/// <summary>
/// Handler for getting the current authenticated user information.
/// </summary>
public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<UserDto>>
{
    /// <inheritdoc />
    public Task<Result<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var userId = request.User.FindFirst("sub")?.Value
            ?? request.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        var email = request.User.FindFirst("email")?.Value;
        var name = request.User.FindFirst("name")?.Value;
        var roles = request.User.FindAll("role").Select(c => c.Value).ToList();

        var userDto = new UserDto
        {
            UserId = userId,
            Email = email,
            Name = name,
            Roles = roles,
            IsAuthenticated = true
        };

        return Task.FromResult(Result.Success(userDto));
    }
}

