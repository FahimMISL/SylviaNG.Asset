using RMS.Domain.Common;
using RMS.Domain.Enums;

namespace RMS.Domain.Entities;

/// <summary>
/// Per-stage-instance progress. Concurrency token is the Npgsql xmin system column
/// (.UseXminAsConcurrencyToken() in the EF config, no extra column) - the mechanism behind
/// double-action protection: a concurrent second action on the same row surfaces as
/// DbUpdateConcurrencyException, caught and rethrown as ConflictException.
/// A SentBack -> Resubmit creates a fresh instance for the same StageOrder; prior instances
/// stay in history (enforced by a filtered unique index on (ProcessId, StageOrder) WHERE
/// Status IN Pending/InProgress/ClarificationRequested - only one *active* instance at a time).
/// </summary>
public class RequisitionApproval : AuditableEntity
{
    public Guid RequisitionApprovalProcessId { get; set; }
    public RequisitionApprovalProcess? RequisitionApprovalProcess { get; set; }

    public Guid ApprovalWorkflowStageId { get; set; }
    public ApprovalWorkflowStage? ApprovalWorkflowStage { get; set; }

    /// <summary>Denormalized for ordering without a join back to the stage.</summary>
    public int StageOrder { get; set; }

    public RequisitionApprovalStatus Status { get; set; } = RequisitionApprovalStatus.Pending;

    public DateTime? SlaStartUtc { get; set; }
    public DateTime? SlaDueUtc { get; set; }
    public DateTime? SlaPausedAtUtc { get; set; }

    /// <summary>Accumulated paused time (clarification requests), added back to SlaDueUtc on resume.</summary>
    public TimeSpan SlaPausedDurationTotal { get; set; } = TimeSpan.Zero;

    public List<RequisitionApprovalAssignment> Assignments { get; set; } = new();
    public List<RequisitionApprovalAction> Actions { get; set; } = new();
}
