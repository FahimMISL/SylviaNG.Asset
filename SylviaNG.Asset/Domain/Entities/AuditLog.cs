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

    /// <summary>
    /// Feature 8: captured directly at write time (not resolved later via EntityId/EntityName joins)
    /// so the audit trail for an entity survives that entity's own deletion - e.g. a "RequisitionDeleted"
    /// row, or any prior history for that requisition, must still be visible after the requisition
    /// itself is gone. Nullable only for the (currently theoretical, single-company-in-practice) case
    /// where no company can be resolved at all.
    /// </summary>
    public Guid? CompanyId { get; set; }

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
