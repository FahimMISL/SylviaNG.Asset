using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.Approvals.Services;

/// <summary>Shared by EscalateApprovalCommandHandler (manual) and SlaBreachEscalationService
/// (automatic) - both "escalate to the stage's configured EscalationApproverUserId/Role" per the
/// Feature 3 plan. Escalating adds the target(s) as additional required approvers on the SAME stage
/// instance rather than creating a new stage - they join the pending reviewers, they don't replace them.</summary>
public static class ApprovalEscalationHelper
{
    public static async Task<List<Guid>> ResolveEscalationTargetsAsync(
        ApprovalWorkflowSlaConfiguration sla, Guid companyId, IUserRepository userRepository, CancellationToken cancellationToken)
    {
        if (sla.EscalationApproverUserId.HasValue)
        {
            var user = await userRepository.GetByIdAsync(sla.EscalationApproverUserId.Value, cancellationToken);
            return user is { IsActive: true } ? [user.Id] : [];
        }

        if (sla.EscalationApproverRole.HasValue)
        {
            var users = await userRepository.GetActiveByRoleAsync(companyId, sla.EscalationApproverRole.Value, cancellationToken);
            return users.Select(u => u.Id).ToList();
        }

        return [];
    }

    /// <summary>Adds one assignment + logs one action per resolved target (skipping anyone already
    /// assigned on this approval so re-escalating doesn't duplicate rows).</summary>
    public static void ApplyEscalation(
        IRequisitionApprovalRepository repository, RequisitionApproval approval, List<Guid> targetUserIds,
        Domain.Enums.ApprovalActionType actionType, Guid? actorUserId, string actorName, string? actorRole, string comment)
    {
        var alreadyAssigned = approval.Assignments.Select(a => a.AssignedUserId).ToHashSet();

        foreach (var targetUserId in targetUserIds)
        {
            if (!alreadyAssigned.Contains(targetUserId))
            {
                repository.AddAssignment(new RequisitionApprovalAssignment
                {
                    RequisitionApprovalId = approval.Id,
                    AssignedUserId = targetUserId,
                    IsRequired = true,
                });
            }

            repository.AddAction(new RequisitionApprovalAction
            {
                RequisitionApprovalId = approval.Id,
                ActionType = actionType,
                ActorUserId = actorUserId,
                ActorName = actorName,
                ActorRole = actorRole,
                Comment = comment,
                EscalatedToUserId = targetUserId,
            });
        }

        if (approval.Status == Domain.Enums.RequisitionApprovalStatus.Pending)
        {
            approval.Status = Domain.Enums.RequisitionApprovalStatus.InProgress;
        }
    }
}
