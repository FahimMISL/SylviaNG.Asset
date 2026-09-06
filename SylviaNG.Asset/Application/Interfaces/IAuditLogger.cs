namespace RMS.Application.Interfaces;

/// <summary>Centralized, cross-cutting audit capture (Master Prompt Section 23) backing US-026.</summary>
public interface IAuditLogger
{
    /// <summary>Actor is taken from the current authenticated request.</summary>
    Task LogAsync(string actionType, string entityName, Guid entityId, string? details = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// For actions where the actor isn't present in the ambient HTTP context yet -
    /// e.g. the login action itself, where the request has no token until after it succeeds.
    /// </summary>
    Task LogAsync(string actionType, string entityName, Guid entityId, Guid actorUserId, string actorName, string actorRole, string? details = null, CancellationToken cancellationToken = default);
}
