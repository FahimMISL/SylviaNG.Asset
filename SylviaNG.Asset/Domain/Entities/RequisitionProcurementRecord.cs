using RMS.Domain.Common;
using RMS.Domain.Enums;

namespace RMS.Domain.Entities;

/// <summary>
/// Feature 5: append-only procurement/fulfillment ledger, one row per StartProcessing/
/// RecordFulfillment/Close action - mirrors RequisitionApprovalAction's role for Feature 3.
/// CreatedAtUtc (from AuditableEntity) doubles as the action timestamp, same convention used
/// throughout the approval ledger.
/// </summary>
public class RequisitionProcurementRecord : AuditableEntity
{
    public Guid RequisitionId { get; set; }
    public Requisition? Requisition { get; set; }

    public ProcurementActionType ActionType { get; set; }

    public Guid ActorUserId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string? ActorRole { get; set; }

    /// <summary>Optional per rule 9 - no minimum length, matching the project-wide decision that
    /// approval/procurement comments are never mandatory.</summary>
    public string? Comment { get; set; }

    /// <summary>Set only on a StartProcessing record: sum(unit price x approved ceiling) across every
    /// item, computed once when procurement begins. Null on RecordFulfillment/Close records.</summary>
    public decimal? TotalProcurementAmount { get; set; }

    public List<RequisitionProcurementLineItem> LineItems { get; set; } = new();
}
