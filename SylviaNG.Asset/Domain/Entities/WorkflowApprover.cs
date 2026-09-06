using RMS.Domain.Enums;

namespace RMS.Domain.Entities;

/// <summary>Child of a Stage - more than one required approver on a stage makes that stage parallel.</summary>
public class WorkflowApprover
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ApprovalWorkflowStageId { get; set; }
    public ApprovalWorkflowStage? ApprovalWorkflowStage { get; set; }

    public ApproverType ApproverType { get; set; }
    public UserRole? ApproverRole { get; set; }
    public Guid? ApproverUserId { get; set; }
    public User? ApproverUser { get; set; }
    public Guid? FallbackApproverUserId { get; set; }
    public User? FallbackApproverUser { get; set; }

    public bool IsRequired { get; set; } = true;
}
