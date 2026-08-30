using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.ApprovalWorkflows.Services;

/// <summary>
/// The unified routing engine per the Feature 3 plan's "one unified engine" design: stages execute
/// in StageOrder; a stage with more than one required approver IS parallel; a stage with conditions
/// IS conditional. RoutingMode never branches this logic - it's UI labeling only.
///
/// Only ONE RequisitionApproval (stage instance) is ever materialized at a time - future stages are
/// resolved lazily as each prior one completes, so a later stage's Cost condition can be evaluated
/// against a real Requisition.EstimatedCost captured by an earlier CapturesEstimatedCost stage rather
/// than the 0 the employee form always submits (see the plan's Context section).
/// </summary>
public class ApprovalWorkflowEngine
{
    private readonly IApprovalWorkflowRepository _workflowRepository;
    private readonly IRequisitionApprovalRepository _requisitionApprovalRepository;
    private readonly IRequisitionRepository _requisitionRepository;
    private readonly IUserRepository _userRepository;

    public ApprovalWorkflowEngine(
        IApprovalWorkflowRepository workflowRepository,
        IRequisitionApprovalRepository requisitionApprovalRepository,
        IRequisitionRepository requisitionRepository,
        IUserRepository userRepository)
    {
        _workflowRepository = workflowRepository;
        _requisitionApprovalRepository = requisitionApprovalRepository;
        _requisitionRepository = requisitionRepository;
        _userRepository = userRepository;
    }

    /// <summary>Called from CreateRequisitionCommandHandler's Submit branch. Resolves the active,
    /// published workflow version for the requisition's (CompanyId, CategoryId), creates the
    /// RequisitionApprovalProcess, transitions Requisition Submitted -> UnderReview via BeginReview,
    /// and starts the first actionable stage (walking past any condition-excluded or fully-self-approval
    /// stages, auto-skipping the latter).</summary>
    public async Task ResolveAndStartAsync(
        Requisition requisition, Guid actorUserId, string actorName, string? actorRole, CancellationToken cancellationToken)
    {
        var version = await _workflowRepository.GetResolvableVersionAsync(requisition.CompanyId, requisition.CategoryId, cancellationToken)
            ?? throw new ConflictException(
                "No active, published approval workflow is configured for this company/category. " +
                "Contact your System Administrator before submitting a requisition.");

        var orderedStages = version.Stages.OrderBy(s => s.StageOrder).ToList();
        if (orderedStages.Count == 0)
        {
            throw new ConflictException("The configured approval workflow has no stages and cannot be used.");
        }

        var process = new RequisitionApprovalProcess
        {
            RequisitionId = requisition.Id,
            ApprovalWorkflowVersionId = version.Id,
            ResolvedAtUtc = DateTime.UtcNow,
        };
        _requisitionApprovalRepository.AddProcess(process);
        requisition.ApprovalProcess = process;

        var beginReviewEntry = requisition.BeginReview(actorUserId, actorName, actorRole);
        _requisitionRepository.AddStatusHistory(beginReviewEntry);

        await AdvanceToNextActionableStageAsync(process, requisition, orderedStages, actorUserId, actorName, actorRole, cancellationToken);
    }

    /// <summary>Called once every required assignment on a stage has acted (IsStageComplete). If the
    /// completed stage CapturesEstimatedCost, applies the captured value to Requisition.EstimatedCost
    /// first, so any later stage's Cost condition sees the real number. Starts the next actionable
    /// stage, or - if none remain - calls Requisition.Approve().</summary>
    public async Task AdvanceAfterApprovalAsync(
        RequisitionApproval completedApproval, Guid actorUserId, string actorName, string? actorRole, CancellationToken cancellationToken)
    {
        var process = completedApproval.RequisitionApprovalProcess
            ?? await _requisitionApprovalRepository.GetProcessByIdAsync(completedApproval.RequisitionApprovalProcessId, cancellationToken)
            ?? throw new NotFoundException(nameof(RequisitionApprovalProcess), completedApproval.RequisitionApprovalProcessId);

        var requisition = process.Requisition
            ?? await _requisitionRepository.GetByIdAsync(process.RequisitionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Requisition), process.RequisitionId);

        if (completedApproval.ApprovalWorkflowStage?.CapturesEstimatedCost == true)
        {
            var capturedCost = completedApproval.Actions
                .Where(a => a.ActionType == ApprovalActionType.Approve && a.CapturedEstimatedCost.HasValue)
                .OrderByDescending(a => a.CreatedAtUtc)
                .Select(a => a.CapturedEstimatedCost!.Value)
                .FirstOrDefault();
            if (capturedCost > 0)
            {
                requisition.EstimatedCost = capturedCost;
            }
        }

        var version = await _workflowRepository.GetVersionByIdAsync(process.ApprovalWorkflowVersionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.ApprovalWorkflowVersion), process.ApprovalWorkflowVersionId);

        var remainingStages = version.Stages
            .Where(s => s.StageOrder > completedApproval.StageOrder)
            .OrderBy(s => s.StageOrder)
            .ToList();

        await AdvanceToNextActionableStageAsync(process, requisition, remainingStages, actorUserId, actorName, actorRole, cancellationToken);
    }

    /// <summary>Called from UpdateRequisitionCommandHandler's Resubmit-after-SentBack branch (NOT
    /// ResolveAndStartAsync - that would create a second RequisitionApprovalProcess for the same
    /// requisition). Reuses the existing process and restarts from whichever stage sent it back: a
    /// fresh RequisitionApproval instance for that same StageOrder. Every prior instance - including
    /// the SentBack one and any earlier completed stages - stays in history untouched, per the plan's
    /// "do not erase previous approval history" rule.</summary>
    public async Task ResumeAfterSendBackAsync(
        Requisition requisition, Guid actorUserId, string actorName, string? actorRole, CancellationToken cancellationToken)
    {
        var process = await _requisitionApprovalRepository.GetProcessByRequisitionIdAsync(requisition.Id, cancellationToken)
            ?? throw new ConflictException("This requisition has no approval process to resume - it was never routed through a workflow.");

        var sentBackStageOrder = process.StageInstances
            .Where(a => a.Status == RequisitionApprovalStatus.SentBack)
            .OrderByDescending(a => a.CreatedAtUtc)
            .Select(a => a.StageOrder)
            .FirstOrDefault();

        var version = await _workflowRepository.GetVersionByIdAsync(process.ApprovalWorkflowVersionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.ApprovalWorkflowVersion), process.ApprovalWorkflowVersionId);

        var candidateStages = version.Stages
            .Where(s => s.StageOrder >= sentBackStageOrder)
            .OrderBy(s => s.StageOrder)
            .ToList();

        var beginReviewEntry = requisition.BeginReview(actorUserId, actorName, actorRole);
        _requisitionRepository.AddStatusHistory(beginReviewEntry);

        process.CompletedAtUtc = null;
        await AdvanceToNextActionableStageAsync(process, requisition, candidateStages, actorUserId, actorName, actorRole, cancellationToken);
    }

    /// <summary>Parallel-stage completion = every required "slot" satisfied. A slot is normally one
    /// assignment (SpecificUser, or a Role that resolved to exactly one user) - that one must act. A
    /// Role that fanned out to several users shares one RoleFanoutGroupId across all of them - that
    /// slot is satisfied the moment ANY ONE of the group acts ("first responder wins"), not all of
    /// them. Ungrouped assignments are their own singleton group (keyed by their own Id) so the same
    /// "any member of the group acted" check covers both cases uniformly.</summary>
    public static bool IsStageComplete(RequisitionApproval approval) =>
        approval.Assignments
            .Where(a => a.IsRequired)
            .GroupBy(a => a.RoleFanoutGroupId ?? a.Id)
            .All(group => group.Any(a => a.HasActed));

    private async Task AdvanceToNextActionableStageAsync(
        RequisitionApprovalProcess process, Requisition requisition, List<ApprovalWorkflowStage> candidateStages,
        Guid actorUserId, string actorName, string? actorRole, CancellationToken cancellationToken)
    {
        foreach (var stage in candidateStages)
        {
            var started = await StartStageAsync(process, requisition, stage, cancellationToken);
            if (started is null || started.Status == RequisitionApprovalStatus.Skipped)
            {
                continue;
            }

            process.CurrentStageOrder = stage.StageOrder;
            return;
        }

        // No stage left applicable/actionable - the requisition is fully approved.
        var approveEntry = requisition.Approve(actorUserId, actorName, actorRole, "All approval stages completed.");
        _requisitionRepository.AddStatusHistory(approveEntry);
        process.CurrentStageOrder = null;
        process.CompletedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Creates and persists a RequisitionApproval instance for one stage, or returns null if
    /// the stage's conditions exclude it entirely (no row created for an inapplicable stage). A stage
    /// whose resolved approvers are ALL the requestor is created but immediately marked Skipped with an
    /// AutoSkip action logged, per the plan's self-approval rule.</summary>
    private async Task<RequisitionApproval?> StartStageAsync(
        RequisitionApprovalProcess process, Requisition requisition, ApprovalWorkflowStage stage, CancellationToken cancellationToken)
    {
        if (!IsStageApplicable(stage, requisition))
        {
            return null;
        }

        var resolvedApprovers = await ResolveApproversAsync(stage, requisition.CompanyId, cancellationToken);

        var approval = new RequisitionApproval
        {
            RequisitionApprovalProcessId = process.Id,
            ApprovalWorkflowStageId = stage.Id,
            StageOrder = stage.StageOrder,
            Status = RequisitionApprovalStatus.Pending,
        };

        if (stage.Sla is not null)
        {
            approval.SlaStartUtc = DateTime.UtcNow;
            approval.SlaDueUtc = ComputeSlaDue(approval.SlaStartUtc.Value, stage.Sla.DurationValue, stage.Sla.DurationUnit);
        }

        _requisitionApprovalRepository.AddApproval(approval);

        var allSelfApproval = resolvedApprovers.All(r => r.UserId == requisition.RequestedByUserId);
        if (allSelfApproval)
        {
            approval.Status = RequisitionApprovalStatus.Skipped;
            _requisitionApprovalRepository.AddAction(new RequisitionApprovalAction
            {
                RequisitionApprovalId = approval.Id,
                ActionType = ApprovalActionType.AutoSkip,
                ActorUserId = null,
                ActorName = "System",
                Comment = "Auto-skipped: every configured approver for this stage is the requestor.",
            });
            return approval;
        }

        foreach (var (userId, isRequired, roleFanoutGroupId) in resolvedApprovers)
        {
            _requisitionApprovalRepository.AddAssignment(new RequisitionApprovalAssignment
            {
                RequisitionApprovalId = approval.Id,
                AssignedUserId = userId,
                IsRequired = isRequired,
                RoleFanoutGroupId = roleFanoutGroupId,
            });
        }

        return approval;
    }

    /// <summary>Called by ApproveApprovalCommandHandler right after marking the acting assignment
    /// HasActed - "first responder wins" for a Role-type approver fanned out to several users: once one
    /// of them acts, the rest of the same RoleFanoutGroupId are closed out (HasActed=true, no separate
    /// ledger action - the group's one Approve action already covers it) so they stop showing as
    /// pending in everyone else's inbox and don't block IsStageComplete.</summary>
    public static void CloseRoleFanoutSiblings(RequisitionApproval approval, RequisitionApprovalAssignment actedAssignment)
    {
        if (actedAssignment.RoleFanoutGroupId is null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var sibling in approval.Assignments)
        {
            if (sibling.Id != actedAssignment.Id
                && sibling.RoleFanoutGroupId == actedAssignment.RoleFanoutGroupId
                && !sibling.HasActed)
            {
                sibling.HasActed = true;
                sibling.ActedAtUtc = now;
            }
        }
    }

    /// <summary>Cost + Category only. No condition rows = always included. Multiple condition rows on
    /// one stage are AND'd - the conservative reading absent an explicit spec for combining them, so a
    /// stage never fires on a partial match the admin didn't configure.</summary>
    private static bool IsStageApplicable(ApprovalWorkflowStage stage, Requisition requisition)
    {
        foreach (var condition in stage.Conditions)
        {
            if (condition.ConditionType == ApprovalConditionType.Cost)
            {
                if (condition.MinCost.HasValue && requisition.EstimatedCost < condition.MinCost.Value)
                {
                    return false;
                }

                if (condition.MaxCost.HasValue && requisition.EstimatedCost > condition.MaxCost.Value)
                {
                    return false;
                }
            }
            else if (condition.CategoryId.HasValue && condition.CategoryId.Value != requisition.CategoryId)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Role -> every currently-active user with that role in the company, but as "any one of
    /// them" (first responder wins), not "all of them must approve": all matching users get an
    /// assignment (so all see it in their inbox and any can act), sharing one RoleFanoutGroupId, and
    /// IsStageComplete only needs one member of that group to have acted - see CloseRoleFanoutSiblings
    /// for how the rest get closed out once someone does. A role resolving to exactly one user needs no
    /// grouping (RoleFanoutGroupId stays null, behaving exactly like a singleton required assignment).
    /// SpecificUser -> that one user if active, else FallbackApproverUserId if configured and active;
    /// always ungrouped - unaffected by this fan-out behavior.</summary>
    private async Task<List<(Guid UserId, bool IsRequired, Guid? RoleFanoutGroupId)>> ResolveApproversAsync(
        ApprovalWorkflowStage stage, Guid companyId, CancellationToken cancellationToken)
    {
        var resolved = new List<(Guid UserId, bool IsRequired, Guid? RoleFanoutGroupId)>();

        foreach (var approver in stage.Approvers)
        {
            if (approver.ApproverType == ApproverType.SpecificUser)
            {
                var userId = approver.ApproverUserId
                    ?? throw new ConflictException($"Approval stage '{stage.Name}' has a Specific User approver with no user configured.");
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user is not null && user.IsActive)
                {
                    resolved.Add((user.Id, approver.IsRequired, null));
                }
                else if (approver.FallbackApproverUserId.HasValue)
                {
                    var fallback = await _userRepository.GetByIdAsync(approver.FallbackApproverUserId.Value, cancellationToken);
                    if (fallback is not null && fallback.IsActive)
                    {
                        resolved.Add((fallback.Id, approver.IsRequired, null));
                    }
                }
            }
            else
            {
                var role = approver.ApproverRole
                    ?? throw new ConflictException($"Approval stage '{stage.Name}' has a Role approver with no role configured.");
                var users = await _userRepository.GetActiveByRoleAsync(companyId, role, cancellationToken);
                var groupId = users.Count > 1 ? Guid.NewGuid() : (Guid?)null;
                resolved.AddRange(users.Select(u => (u.Id, approver.IsRequired, groupId)));
            }
        }

        if (resolved.Count == 0)
        {
            throw new ConflictException(
                $"Approval stage '{stage.Name}' has no active approver available. Configure an active user for this stage before requisitions can be submitted.");
        }

        return resolved.DistinctBy(r => r.UserId).ToList();
    }

    private static DateTime ComputeSlaDue(DateTime startUtc, int durationValue, SlaDurationUnit unit) =>
        unit switch
        {
            SlaDurationUnit.CalendarDays => startUtc.AddDays(durationValue),
            SlaDurationUnit.BusinessHours => AddBusinessHours(startUtc, durationValue),
            _ => startUtc.AddDays(durationValue),
        };

    /// <summary>Simplified business-hours model: skips Saturday/Sunday entirely but doesn't restrict to
    /// a fixed daily window (no working-hours-of-day config exists anywhere in this codebase today).</summary>
    private static DateTime AddBusinessHours(DateTime start, int hours)
    {
        var current = start;
        var remaining = hours;
        while (remaining > 0)
        {
            current = current.AddHours(1);
            if (current.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
            {
                remaining--;
            }
        }

        return current;
    }
}
