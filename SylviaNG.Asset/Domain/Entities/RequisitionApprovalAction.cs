using RMS.Domain.Common;
using RMS.Domain.Enums;

namespace RMS.Domain.Entities;

/// <summary>
/// Append-only granular action ledger - NOT a duplicate of RequisitionStatusHistory. This drives the
/// approval-inbox/detail's stage-by-stage view and records every approver action, including ones
/// that don't change Requisition.Status (Delegate, Escalate, an intra-parallel-stage Approve that
/// doesn't complete the stage yet, clarification request/response, AutoSkip). CreatedAtUtc (from
/// AuditableEntity) doubles as the action timestamp, same convention RequisitionStatusHistoryDto
/// already uses for its own CreatedAtUtc.
/// </summary>
public class RequisitionApprovalAction : AuditableEntity
{
    public Guid RequisitionApprovalId { get; set; }
    public RequisitionApproval? RequisitionApproval { get; set; }

    public ApprovalActionType ActionType { get; set; }

    /// <summary>Nullable for System-originated actions (e.g. SlaBreachEscalation).</summary>
    public Guid? ActorUserId { get; set; }
    public string ActorName { get; set; } = "System";
    public string? ActorRole { get; set; }

    public string? Comment { get; set; }

    public Guid? DelegatedToUserId { get; set; }
    public Guid? EscalatedToUserId { get; set; }

    /// <summary>Set only on the Approve action of a stage flagged CapturesEstimatedCost.</summary>
    public decimal? CapturedEstimatedCost { get; set; }

    public List<PartialApprovalDecision> PartialDecisions { get; set; } = new();
}
