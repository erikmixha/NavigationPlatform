using MediatR;
using Shared.Common.Result;

namespace Gateway.Application.Commands.UpdateUserStatus;

/// <summary>
/// Command to update a user's account status.
/// </summary>
public sealed record UpdateUserStatusCommand : IRequest<Result>
{
    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the new status (Active or Suspended).
    /// </summary>
    public string NewStatus { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the user making the change.
    /// </summary>
    public string ChangedByUserId { get; init; } = string.Empty;
}

