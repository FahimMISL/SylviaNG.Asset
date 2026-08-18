namespace RMS.Domain.Entities;

/// <summary>
/// 1:1 with Requisition - captures the resolved ApprovalWorkflowVersion, mirroring
/// Requisition.CategoryVersionNumber's snapshot precedent: once resolved at submission time, this
/// requisition rides that exact version for its whole lifecycle even if a new version is later
/// published for the workflow.
/// </summary>
public class RequisitionApprovalProcess
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RequisitionId { get; set; }
    public Requisition? Requisition { get; set; }

    public Guid ApprovalWorkflowVersionId { get; set; }
    public ApprovalWorkflowVersion? ApprovalWorkflowVersion { get; set; }

    public DateTime ResolvedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Null once the process is terminal (Requisition reached Approved/Rejected/PartiallyApproved/SentBack).</summary>
    public int? CurrentStageOrder { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public List<RequisitionApproval> StageInstances { get; set; } = new();
}
