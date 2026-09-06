using RMS.Application.Features.Approvals.Services;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Requisitions.Services;

/// <summary>
/// Feature 10 (US-032): the single source of truth for "can this user see this requisition" - extracted
/// out of GetRequisitionByIdQueryHandler (which had it inline) so attachment download/upload/delete no
/// longer use a narrower owner-only rule than the requisition detail page itself does. Both call this
/// one helper now, so the two access rules can never drift again.
/// </summary>
public static class RequisitionAccessHelper
{
    public static readonly HashSet<RequisitionStatus> ProcurementPipelineStatuses =
    [
        RequisitionStatus.Approved, RequisitionStatus.PartiallyApproved, RequisitionStatus.InProcurement,
        RequisitionStatus.PartiallyFulfilled, RequisitionStatus.Fulfilled, RequisitionStatus.Closed,
    ];

    /// <summary>Owner / any approver who's ever had an assignment on it (delegation-aware) / any
    /// Procurement Officer once it's in the pipeline (Feature 5 rule 1: no per-user assignment) /
    /// DepartmentHead for a requisition raised within their own department / SystemAdmin.</summary>
    public static async Task<bool> CanAccessAsync(
        Requisition requisition, Guid userId, ICurrentUserService currentUser,
        IApprovalDelegationRepository delegationRepository, CancellationToken cancellationToken)
    {
        var isOwner = requisition.RequestedByUserId == userId;
        var isSystemAdmin = currentUser.IsInRole(UserRole.SystemAdmin);
        var isAuthorizedApprover = !isOwner && !isSystemAdmin
            && await HasEverHadAnAssignmentAsync(requisition, userId, delegationRepository, cancellationToken);
        var isProcurementOfficer = !isOwner && !isSystemAdmin && !isAuthorizedApprover
            && currentUser.IsInRole(UserRole.ProcurementOfficer) && ProcurementPipelineStatuses.Contains(requisition.Status);
        var isDepartmentHead = !isOwner && !isSystemAdmin && !isAuthorizedApprover && !isProcurementOfficer
            && currentUser.IsInRole(UserRole.DepartmentHead)
            && currentUser.Department is not null
            && requisition.RequestedByUser?.Department == currentUser.Department;

        return isOwner || isSystemAdmin || isAuthorizedApprover || isProcurementOfficer || isDepartmentHead;
    }

    /// <summary>Has ever had a RequisitionApprovalAssignment (effective, delegation-aware) on this
    /// requisition's approval process - either as the original assignee (who keeps read access even
    /// while an out-of-office delegation is active, since delegation never mutates the assignment row),
    /// or as someone currently standing in for one via an active ApprovalDelegation.</summary>
    private static async Task<bool> HasEverHadAnAssignmentAsync(
        Requisition requisition, Guid userId, IApprovalDelegationRepository delegationRepository, CancellationToken cancellationToken)
    {
        if (requisition.ApprovalProcess is null)
        {
            return false;
        }

        var assignedUserIds = requisition.ApprovalProcess.StageInstances
            .SelectMany(s => s.Assignments)
            .Select(a => a.AssignedUserId)
            .Distinct()
            .ToList();

        if (assignedUserIds.Contains(userId))
        {
            return true;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        foreach (var assignedUserId in assignedUserIds)
        {
            var effectiveAssignee = await ApprovalAuthorizationHelper.ResolveEffectiveAssigneeAsync(
                assignedUserId, delegationRepository, today, cancellationToken);
            if (effectiveAssignee == userId)
            {
                return true;
            }
        }

        return false;
    }
}
