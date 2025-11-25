using Gateway.Application.DTOs;
using Gateway.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Common.Result;

namespace Gateway.Application.Commands.RefreshToken;

/// <summary>
/// Handler for refreshing access tokens.
/// </summary>
public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<TokenResponse>>
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokenCommandHandler"/> class.
    /// </summary>
    public RefreshTokenCommandHandler(
        IAuthenticationService authenticationService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<TokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            return Result.Failure<TokenResponse>(
                new Shared.Common.Result.Error("Token.Invalid", "Refresh token is required"));
        }

        var tokenResponse = await _authenticationService.RefreshTokenAsync(request.RefreshToken, cancellationToken);
        
        if (tokenResponse == null)
        {
            return Result.Failure<TokenResponse>(
                new Shared.Common.Result.Error("Token.RefreshFailed", "Failed to refresh token"));
        }

        return Result.Success(tokenResponse);
    }
}

