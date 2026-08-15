namespace RMS.Domain.Entities;

/// <summary>
/// Minimal append-only audit capture per US-026, used as the shared,
/// cross-cutting logging mechanism (Master Prompt Section 23) rather than a
/// bespoke log per feature. Cross-requisition querying/export/retention
/// policy is Feature 10 scope and is deferred.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ActorUserId { get; set; }
    public string ActorName { get; set; } = "System";
    public string? ActorRole { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? Details { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
}
