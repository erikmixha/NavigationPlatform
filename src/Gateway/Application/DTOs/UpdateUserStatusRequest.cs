namespace Gateway.Application.DTOs;

/// <summary>
/// Request model for updating a user's account status.
/// </summary>
public sealed record UpdateUserStatusRequest
{
    /// <summary>
    /// Gets or sets the new status (Active or Suspended).
    /// </summary>
    public string Status { get; init; } = string.Empty;
}

