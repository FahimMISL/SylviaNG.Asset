using RMS.Domain.Enums;

namespace RMS.Domain.Entities;

/// <summary>Cost + Category only - see the Feature 3 plan's "Company condition is redundant, dropped"
/// correction. No condition rows on a stage = stage always included (unconditional).</summary>
public class ApprovalWorkflowStageCondition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ApprovalWorkflowStageId { get; set; }
    public ApprovalWorkflowStage? ApprovalWorkflowStage { get; set; }

    public ApprovalConditionType ConditionType { get; set; }

    public decimal? MinCost { get; set; }
    public decimal? MaxCost { get; set; }

    public Guid? CategoryId { get; set; }
    public RequisitionCategory? Category { get; set; }
}
