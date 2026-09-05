namespace RMS.Application.Interfaces;

/// <summary>Centralized, cross-cutting audit capture (Master Prompt Section 23) backing US-026.</summary>
public interface IAuditLogger
{
    /// <summary>Actor is taken from the current authenticated request. `target` is the human-readable
    /// name of whoever/whatever this action was actually done TO (e.g. the affected user's name for
    /// UserRoleChanged) - optional and appended last, deliberately, so every one of this method's
    /// ~50 existing positional call sites (none of which pass a trailing CancellationToken by name)
    /// keeps compiling unchanged; they simply log no target (renders as "—") until updated.</summary>
    Task LogAsync(string actionType, string entityName, Guid entityId, string? details = null, CancellationToken cancellationToken = default, string? target = null);

    /// <summary>
    /// For actions where the actor isn't present in the ambient HTTP context yet -
    /// e.g. the login action itself, where the request has no token until after it succeeds.
    /// </summary>
    Task LogAsync(string actionType, string entityName, Guid entityId, Guid actorUserId, string actorName, string actorRole, string? details = null, CancellationToken cancellationToken = default, string? target = null);
}
