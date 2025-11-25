using MediatR;
using Shared.Common.Result;

namespace Gateway.Application.Commands.RefreshToken;

/// <summary>
/// Command to refresh an access token using a refresh token.
/// </summary>
public sealed record RefreshTokenCommand : IRequest<Result<DTOs.TokenResponse>>
{
    /// <summary>
    /// Gets or sets the refresh token.
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;
}

