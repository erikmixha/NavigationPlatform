using Gateway.Domain.Entities;
using Gateway.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gateway.Infrastructure.Services;

public interface IUserStatusAuditService
{
    Task AddAuditAsync(string userId, string previousStatus, string newStatus, string changedByUserId, CancellationToken cancellationToken = default);
}

/// <remarks>
/// Excluded from code coverage: Infrastructure service for audit logging.
/// Audit operations are tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Infrastructure service - audit logging tested via integration tests.")]
public sealed class UserStatusAuditService : IUserStatusAuditService
{
    private readonly GatewayDbContext _context;
    private readonly ILogger<UserStatusAuditService> _logger;

    public UserStatusAuditService(
        GatewayDbContext context,
        ILogger<UserStatusAuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddAuditAsync(
        string userId,
        string previousStatus,
        string newStatus,
        string changedByUserId,
        CancellationToken cancellationToken = default)
    {
        var audit = new UserStatusAudit(userId, previousStatus, newStatus, changedByUserId);
        await _context.UserStatusAudits.AddAsync(audit, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User status audit recorded: User {UserId}, {PreviousStatus} -> {NewStatus}, Changed by {ChangedByUserId}",
            userId,
            previousStatus,
            newStatus,
            changedByUserId);
    }
}

