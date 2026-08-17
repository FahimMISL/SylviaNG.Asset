using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Infrastructure.Data;

namespace RMS.Infrastructure.Services;

public class AuditLogger : IAuditLogger
{
    private readonly RmsDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public AuditLogger(RmsDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public Task LogAsync(string actionType, string entityName, Guid entityId, string? details = null, CancellationToken cancellationToken = default) =>
        WriteAsync(
            actionType, entityName, entityId,
            _currentUser.UserId, _currentUser.FullName ?? "System", _currentUser.Role?.ToString(),
            details, cancellationToken);

    public Task LogAsync(string actionType, string entityName, Guid entityId, Guid actorUserId, string actorName, string actorRole, string? details = null, CancellationToken cancellationToken = default) =>
        WriteAsync(actionType, entityName, entityId, actorUserId, actorName, actorRole, details, cancellationToken);

    private async Task WriteAsync(string actionType, string entityName, Guid entityId, Guid? actorUserId, string actorName, string? actorRole, string? details, CancellationToken cancellationToken)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            ActorName = actorName,
            ActorRole = actorRole,
            ActionType = actionType,
            EntityName = entityName,
            EntityId = entityId,
            Details = details,
            IpAddress = _currentUser.IpAddress,
            TimestampUtc = DateTime.UtcNow,
        });

        // Audit entries are written eagerly so they persist even if the caller's
        // SaveChangesAsync happens later in the same unit of work.
        await _context.SaveChangesAsync(cancellationToken);
    }
}
