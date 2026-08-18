using RMS.Domain.Entities;

namespace RMS.Application.Features.ApprovalWorkflows.DTOs;

public record WorkflowApproverDto(
    Guid Id, string ApproverType, string? ApproverRole, Guid? ApproverUserId, string? ApproverUserName,
    Guid? FallbackApproverUserId, bool IsRequired)
{
    public static WorkflowApproverDto FromEntity(WorkflowApprover a) => new(
        a.Id, a.ApproverType.ToString(), a.ApproverRole?.ToString(), a.ApproverUserId, a.ApproverUser?.FullName,
        a.FallbackApproverUserId, a.IsRequired);
}

public record ApprovalWorkflowStageConditionDto(Guid Id, string ConditionType, decimal? MinCost, decimal? MaxCost, Guid? CategoryId)
{
    public static ApprovalWorkflowStageConditionDto FromEntity(ApprovalWorkflowStageCondition c) => new(
        c.Id, c.ConditionType.ToString(), c.MinCost, c.MaxCost, c.CategoryId);
}

public record ApprovalWorkflowSlaDto(
    int DurationValue, string DurationUnit, bool Reminder50PercentEnabled, bool Reminder80PercentEnabled,
    bool EscalateOnBreach, string? EscalationApproverRole, Guid? EscalationApproverUserId)
{
    public static ApprovalWorkflowSlaDto FromEntity(ApprovalWorkflowSlaConfiguration s) => new(
        s.DurationValue, s.DurationUnit.ToString(), s.Reminder50PercentEnabled, s.Reminder80PercentEnabled,
        s.EscalateOnBreach, s.EscalationApproverRole?.ToString(), s.EscalationApproverUserId);
}

public record ApprovalWorkflowStageDto(
    Guid Id, int StageOrder, string Name, bool CapturesEstimatedCost,
    List<WorkflowApproverDto> Approvers, List<ApprovalWorkflowStageConditionDto> Conditions, ApprovalWorkflowSlaDto? Sla)
{
    public static ApprovalWorkflowStageDto FromEntity(ApprovalWorkflowStage s) => new(
        s.Id, s.StageOrder, s.Name, s.CapturesEstimatedCost,
        s.Approvers.Select(WorkflowApproverDto.FromEntity).ToList(),
        s.Conditions.Select(ApprovalWorkflowStageConditionDto.FromEntity).ToList(),
        s.Sla is null ? null : ApprovalWorkflowSlaDto.FromEntity(s.Sla));
}

public record ApprovalWorkflowVersionDto(
    Guid Id, int VersionNumber, string RoutingMode, bool AppliesToAllCategories, bool IsPublished,
    DateTime? PublishedAtUtc, string? Notes, List<Guid> CategoryIds, List<ApprovalWorkflowStageDto> Stages)
{
    public static ApprovalWorkflowVersionDto FromEntity(ApprovalWorkflowVersion v) => new(
        v.Id, v.VersionNumber, v.RoutingMode.ToString(), v.AppliesToAllCategories, v.IsPublished, v.PublishedAtUtc, v.Notes,
        v.CategoryLinks.Select(l => l.RequisitionCategoryId).ToList(),
        v.Stages.OrderBy(s => s.StageOrder).Select(ApprovalWorkflowStageDto.FromEntity).ToList());
}

public record ApprovalWorkflowDto(
    Guid Id, string Name, string? Description, bool IsActive, int CurrentVersionNumber, List<ApprovalWorkflowVersionDto> Versions)
{
    public static ApprovalWorkflowDto FromEntity(ApprovalWorkflow w) => new(
        w.Id, w.Name, w.Description, w.IsActive, w.CurrentVersionNumber,
        w.Versions.OrderByDescending(v => v.VersionNumber).Select(ApprovalWorkflowVersionDto.FromEntity).ToList());
}

public record ApprovalWorkflowSummaryDto(Guid Id, string Name, bool IsActive, int CurrentVersionNumber, int StageCount)
{
    public static ApprovalWorkflowSummaryDto FromEntity(ApprovalWorkflow w)
    {
        var current = w.Versions.FirstOrDefault(v => v.VersionNumber == w.CurrentVersionNumber);
        return new(w.Id, w.Name, w.IsActive, w.CurrentVersionNumber, current?.Stages.Count ?? 0);
    }
}
