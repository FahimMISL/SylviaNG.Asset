namespace RMS.Domain.Entities;

/// <summary>
/// One stage in a workflow version. Stages execute in StageOrder. More than one required
/// approver on a stage IS the "parallel" behavior; conditions attached to a stage IS the
/// "conditional" behavior - see the Feature 3 plan's unified-engine design.
/// </summary>
public class ApprovalWorkflowStage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ApprovalWorkflowVersionId { get; set; }
    public ApprovalWorkflowVersion? ApprovalWorkflowVersion { get; set; }

    public int StageOrder { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>When true, this stage's Approve action collects Requisition.EstimatedCost from the
    /// approver (the employee form never captures it - see the Feature 3 plan's Context section).</summary>
    public bool CapturesEstimatedCost { get; set; }

    public List<WorkflowApprover> Approvers { get; set; } = new();
    public List<ApprovalWorkflowStageCondition> Conditions { get; set; } = new();
    public ApprovalWorkflowSlaConfiguration? Sla { get; set; }
}
