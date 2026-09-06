using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.Approvals.Services;

/// <summary>Shared by every approval-action handler's authorization check - delegation-aware per the
/// plan: effectiveAssignee = ActiveDelegation(assignedUserId, today)?.DelegateUserId ?? assignedUserId.
/// Delegation NEVER mutates the assignment row itself - this is resolved fresh on every call.</summary>
public static class ApprovalAuthorizationHelper
{
    /// <summary>Finds the not-yet-acted assignment on this approval whose effective (delegation-resolved)
    /// assignee is the current user. Throws ForbiddenException if none match - "not your approval to
    /// act on" per the plan.</summary>
    public static async Task<RequisitionApprovalAssignment> GetActionableAssignmentAsync(
        RequisitionApproval approval, Guid currentUserId, IApprovalDelegationRepository delegationRepository, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var assignment in approval.Assignments.Where(a => !a.HasActed))
        {
            var effectiveAssignee = await ResolveEffectiveAssigneeAsync(assignment.AssignedUserId, delegationRepository, today, cancellationToken);
            if (effectiveAssignee == currentUserId)
            {
                return assignment;
            }
        }

        throw new ForbiddenException("You are not an authorized approver for this action.");
    }

    public static async Task<Guid> ResolveEffectiveAssigneeAsync(
        Guid assignedUserId, IApprovalDelegationRepository delegationRepository, DateOnly today, CancellationToken cancellationToken)
    {
        var activeDelegation = await delegationRepository.GetActiveOnAsync(assignedUserId, today, cancellationToken);
        return activeDelegation?.DelegateUserId ?? assignedUserId;
    }

    /// <summary>Non-throwing companion to GetActionableAssignmentAsync, for read-only surfaces (the
    /// requisition detail DTO) that need to know "would this action be allowed" without attempting it.
    /// Uses the EXACT SAME effective-assignee resolution as every real approve/reject/etc. handler, so
    /// the frontend never has to duplicate delegation-aware authorization itself - it just reads this
    /// one authoritative flag. Looks only at the process's current stage (CurrentStageOrder) while it's
    /// actionable (Pending/InProgress) - null/terminal/ClarificationRequested all correctly yield false.</summary>
    public static async Task<bool> IsCurrentUserActionableAsync(
        RequisitionApprovalProcess? process, Guid? currentUserId, IApprovalDelegationRepository delegationRepository, CancellationToken cancellationToken)
    {
        if (process is null || currentUserId is null || process.CurrentStageOrder is null)
        {
            return false;
        }

        var currentApproval = process.StageInstances.FirstOrDefault(s =>
            s.StageOrder == process.CurrentStageOrder
            && (s.Status == Domain.Enums.RequisitionApprovalStatus.Pending || s.Status == Domain.Enums.RequisitionApprovalStatus.InProgress));
        if (currentApproval is null)
        {
            return false;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        foreach (var assignment in currentApproval.Assignments.Where(a => !a.HasActed))
        {
            var effectiveAssignee = await ResolveEffectiveAssigneeAsync(assignment.AssignedUserId, delegationRepository, today, cancellationToken);
            if (effectiveAssignee == currentUserId.Value)
            {
                return true;
            }
        }

        return false;
    }
}
