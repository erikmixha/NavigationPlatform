using Gateway.Application.Interfaces;
using Gateway.Infrastructure.Services;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Common.Result;
using Shared.Messaging.Events;

namespace Gateway.Application.Commands.UpdateUserStatus;

/// <summary>
/// Handler for updating user account status.
/// </summary>
public sealed class UpdateUserStatusCommandHandler : IRequestHandler<UpdateUserStatusCommand, Result>
{
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IUserStatusAuditService _auditService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<UpdateUserStatusCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateUserStatusCommandHandler"/> class.
    /// </summary>
    public UpdateUserStatusCommandHandler(
        IKeycloakUserService keycloakUserService,
        IUserStatusAuditService auditService,
        IPublishEndpoint publishEndpoint,
        ILogger<UpdateUserStatusCommandHandler> logger)
    {
        _keycloakUserService = keycloakUserService;
        _auditService = auditService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(UpdateUserStatusCommand request, CancellationToken cancellationToken)
    {
        if (!IsValidStatus(request.NewStatus))
        {
            return Result.Failure(new Shared.Common.Result.Error(
                "Status.Invalid",
                "Invalid status. Must be one of: Active, Suspended"));
        }

        var previousStatus = await _keycloakUserService.GetUserStatusAsync(request.UserId, cancellationToken);
        var enabled = request.NewStatus == "Active";
        
        var success = await _keycloakUserService.UpdateUserStatusAsync(request.UserId, enabled, cancellationToken);
        if (!success)
        {
            return Result.Failure(new Shared.Common.Result.Error(
                "User.NotFound",
                $"User with ID {request.UserId} not found in Keycloak"));
        }

        await _auditService.AddAuditAsync(
            request.UserId,
            previousStatus,
            request.NewStatus,
            request.ChangedByUserId,
            cancellationToken);

        await _publishEndpoint.Publish(new UserStatusChangedEvent
        {
            UserId = request.UserId,
            PreviousStatus = previousStatus,
            NewStatus = request.NewStatus,
            ChangedByUserId = request.ChangedByUserId,
            OccurredOnUtc = DateTime.UtcNow
        }, cancellationToken);

        _logger.LogInformation(
            "User {UserId} status updated from {PreviousStatus} to {NewStatus} by admin {ChangedByUserId}",
            request.UserId,
            previousStatus,
            request.NewStatus,
            request.ChangedByUserId);

        return Result.Success();
    }

    private static bool IsValidStatus(string status)
    {
        return status is "Active" or "Suspended";
    }
}
