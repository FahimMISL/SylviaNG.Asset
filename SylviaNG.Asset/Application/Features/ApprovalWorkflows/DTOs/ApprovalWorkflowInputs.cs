using RMS.Domain.Enums;

namespace RMS.Application.Features.ApprovalWorkflows.DTOs;

public record WorkflowApproverInput(
    ApproverType ApproverType,
    UserRole? ApproverRole,
    Guid? ApproverUserId,
    Guid? FallbackApproverUserId,
    bool IsRequired);

public record ApprovalWorkflowStageConditionInput(
    ApprovalConditionType ConditionType,
    decimal? MinCost,
    decimal? MaxCost,
    Guid? CategoryId);

public record ApprovalWorkflowSlaInput(
    int DurationValue,
    SlaDurationUnit DurationUnit,
    bool Reminder50PercentEnabled,
    bool Reminder80PercentEnabled,
    bool EscalateOnBreach,
    UserRole? EscalationApproverRole,
    Guid? EscalationApproverUserId);

public record ApprovalWorkflowStageInput(
    int StageOrder,
    string Name,
    bool CapturesEstimatedCost,
    List<WorkflowApproverInput> Approvers,
    List<ApprovalWorkflowStageConditionInput> Conditions,
    ApprovalWorkflowSlaInput? Sla);
