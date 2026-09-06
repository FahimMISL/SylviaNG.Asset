using RMS.Application.Features.Approvals.Services;
using RMS.Domain.Entities;

namespace RMS.Application.Features.Approvals.DTOs;

public record PartialApprovalDecisionDto(Guid RequisitionItemId, string ItemName, int ApprovedQuantity, int DeclinedQuantity, string? DeclineReason)
{
    public static PartialApprovalDecisionDto FromEntity(PartialApprovalDecision d) => new(
        d.RequisitionItemId, d.RequisitionItem?.ItemName ?? string.Empty, d.ApprovedQuantity, d.DeclinedQuantity, d.DeclineReason);
}

public record ApprovalActionDto(
    Guid Id, string ActionType, Guid? ActorUserId, string ActorName, string? ActorRole, string? Comment,
    Guid? DelegatedToUserId, Guid? EscalatedToUserId, decimal? CapturedEstimatedCost, DateTime TimestampUtc,
    List<PartialApprovalDecisionDto> PartialDecisions)
{
    public static ApprovalActionDto FromEntity(RequisitionApprovalAction a) => new(
        a.Id, a.ActionType.ToString(), a.ActorUserId, a.ActorName, a.ActorRole, a.Comment,
        a.DelegatedToUserId, a.EscalatedToUserId, a.CapturedEstimatedCost, a.CreatedAtUtc,
        a.PartialDecisions.Select(PartialApprovalDecisionDto.FromEntity).ToList());
}

public record ApprovalAssignmentDto(
    Guid Id, Guid AssignedUserId, string AssignedUserName, bool IsRequired, bool HasActed, DateTime? ActedAtUtc)
{
    public static ApprovalAssignmentDto FromEntity(RequisitionApprovalAssignment a) => new(
        a.Id, a.AssignedUserId, a.AssignedUser?.FullName ?? string.Empty, a.IsRequired, a.HasActed, a.ActedAtUtc);
}

public record ApprovalStageInstanceDto(
    Guid Id, int StageOrder, string StageName, string Status, bool CapturesEstimatedCost,
    DateTime? SlaStartUtc, DateTime? SlaDueUtc, DateTime? SlaPausedAtUtc, string SlaState,
    List<ApprovalAssignmentDto> Assignments, List<ApprovalActionDto> Actions)
{
    public static ApprovalStageInstanceDto FromEntity(RequisitionApproval a, DateTime nowUtc)
    {
        var slaState = SlaStateCalculator.Compute(a.SlaStartUtc, a.SlaDueUtc, a.SlaPausedAtUtc, nowUtc);
        return new(
            a.Id, a.StageOrder, a.ApprovalWorkflowStage?.Name ?? string.Empty, a.Status.ToString(),
            a.ApprovalWorkflowStage?.CapturesEstimatedCost ?? false,
            a.SlaStartUtc, a.SlaDueUtc, a.SlaPausedAtUtc, slaState.ToString(),
            a.Assignments.Select(ApprovalAssignmentDto.FromEntity).ToList(),
            a.Actions.OrderBy(x => x.CreatedAtUtc).Select(ApprovalActionDto.FromEntity).ToList());
    }
}

/// <summary>Nested on RequisitionDto for approver access, per the Feature 3 plan's extension of
/// GetRequisitionByIdQueryHandler - read-only, requisition fields themselves are untouched here.
/// CurrentUserCanAct is computed by the caller via ApprovalAuthorizationHelper.IsCurrentUserActionableAsync
/// - the exact same delegation-aware effective-assignee resolution the real approve/reject/etc. command
/// handlers use to authorize - so the frontend has one authoritative flag instead of re-deriving
/// "am I an approver" itself from a raw AssignedUserId match (which would miss active delegations).</summary>
public record ApprovalProcessDto(
    Guid Id, string WorkflowName, int WorkflowVersionNumber, int? CurrentStageOrder, DateTime? CompletedAtUtc,
    bool CurrentUserCanAct, List<ApprovalStageInstanceDto> Stages)
{
    public static ApprovalProcessDto FromEntity(RequisitionApprovalProcess p, DateTime nowUtc, bool currentUserCanAct = false) => new(
        p.Id,
        p.ApprovalWorkflowVersion?.ApprovalWorkflow?.Name ?? string.Empty,
        p.ApprovalWorkflowVersion?.VersionNumber ?? 0,
        p.CurrentStageOrder,
        p.CompletedAtUtc,
        currentUserCanAct,
        p.StageInstances.OrderBy(s => s.StageOrder).Select(s => ApprovalStageInstanceDto.FromEntity(s, nowUtc)).ToList());
}
