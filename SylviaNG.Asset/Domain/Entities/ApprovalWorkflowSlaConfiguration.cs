using RMS.Domain.Enums;

namespace RMS.Domain.Entities;

/// <summary>1:1 child of a Stage, separate entity - mirrors CategoryFieldValidationRule being a
/// separate child of CategoryFieldDefinition. Used for both automatic SLA-breach escalation and the
/// manual Escalate action's target.</summary>
public class ApprovalWorkflowSlaConfiguration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ApprovalWorkflowStageId { get; set; }
    public ApprovalWorkflowStage? ApprovalWorkflowStage { get; set; }

    public int DurationValue { get; set; }
    public SlaDurationUnit DurationUnit { get; set; }

    public bool Reminder50PercentEnabled { get; set; }
    public bool Reminder80PercentEnabled { get; set; }

    public bool EscalateOnBreach { get; set; }
    public UserRole? EscalationApproverRole { get; set; }
    public Guid? EscalationApproverUserId { get; set; }
    public User? EscalationApproverUser { get; set; }
}
