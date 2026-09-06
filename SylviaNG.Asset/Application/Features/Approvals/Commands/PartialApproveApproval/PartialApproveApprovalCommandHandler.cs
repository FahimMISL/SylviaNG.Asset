using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.ApprovalWorkflows.Services;
using RMS.Application.Features.Approvals.Services;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Approvals.Commands.PartialApproveApproval;

public class PartialApproveApprovalCommandHandler : IRequestHandler<PartialApproveApprovalCommand>
{
    private readonly IRequisitionApprovalRepository _requisitionApprovalRepository;
    private readonly IApprovalDelegationRepository _delegationRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApprovalWorkflowEngine _engine;
    private readonly INotificationService _notificationService;

    public PartialApproveApprovalCommandHandler(
        IRequisitionApprovalRepository requisitionApprovalRepository, IApprovalDelegationRepository delegationRepository,
        ICurrentUserService currentUser, IAuditLogger auditLogger, IUnitOfWork unitOfWork, ApprovalWorkflowEngine engine,
        INotificationService notificationService)
    {
        _requisitionApprovalRepository = requisitionApprovalRepository;
        _delegationRepository = delegationRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
        _engine = engine;
        _notificationService = notificationService;
    }

    public async Task Handle(PartialApproveApprovalCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();
        var actorName = _currentUser.FullName ?? "Unknown";
        var actorRole = _currentUser.Role?.ToString();

        var approval = await _requisitionApprovalRepository.GetApprovalByIdAsync(request.ApprovalId, cancellationToken)
            ?? throw new NotFoundException(nameof(RequisitionApproval), request.ApprovalId);

        if (approval.Status is not (RequisitionApprovalStatus.Pending or RequisitionApprovalStatus.InProgress))
        {
            throw new ConflictException("This approval stage is no longer awaiting action.");
        }

        var assignment = await ApprovalAuthorizationHelper.GetActionableAssignmentAsync(
            approval, userId, _delegationRepository, cancellationToken);

        // A partial approval must be a complete split of each item's requested quantity: approved and
        // declined both non-zero (this is what makes it "partial" rather than a plain Approve/Reject),
        // together adding up to exactly what was requested - never more, never less. Checked here
        // rather than in the FluentValidation validator because it needs each item's real requested
        // quantity, which only exists on the already-loaded Requisition, not on the request DTO itself.
        var requisitionItems = approval.RequisitionApprovalProcess!.Requisition!.Items.ToDictionary(i => i.Id);
        var failures = new List<ValidationFailure>();

        foreach (var decision in request.Decisions)
        {
            if (!requisitionItems.TryGetValue(decision.RequisitionItemId, out var item))
            {
                failures.Add(new ValidationFailure("Decisions", "One of the submitted items does not belong to this requisition."));
                continue;
            }

            if (decision.ApprovedQuantity <= 0 || decision.DeclinedQuantity <= 0
                || decision.ApprovedQuantity + decision.DeclinedQuantity != item.Quantity)
            {
                failures.Add(new ValidationFailure("Decisions",
                    $"{item.ItemName}: approved and declined quantities must both be greater than zero, and their total must equal the requested quantity ({item.Quantity})."));
            }
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        assignment.HasActed = true;
        assignment.ActedAtUtc = DateTime.UtcNow;

        // No dedicated "PartiallyApproved" value exists on RequisitionApprovalStatus (only on the
        // Requisition-level status) - Approved is the conservative choice: the stage instance itself
        // did resolve/complete, the partial outcome lives on the PartialApprovalDecision rows and the
        // ledger action, exactly like the Requisition-level PartiallyApproved status already coexists
        // with a normal completed approval trail.
        approval.Status = RequisitionApprovalStatus.Approved;

        var action = new RequisitionApprovalAction
        {
            RequisitionApprovalId = approval.Id,
            ActionType = ApprovalActionType.PartialApprove,
            ActorUserId = userId,
            ActorName = actorName,
            ActorRole = actorRole,
            Comment = request.Comment,
        };

        foreach (var decision in request.Decisions)
        {
            action.PartialDecisions.Add(new PartialApprovalDecision
            {
                RequisitionItemId = decision.RequisitionItemId,
                ApprovedQuantity = decision.ApprovedQuantity,
                DeclinedQuantity = decision.DeclinedQuantity,
                DeclineReason = decision.DeclineReason,
            });
        }

        _requisitionApprovalRepository.AddAction(action);

        var requisition = approval.RequisitionApprovalProcess!.Requisition!;

        // A partial decision only ever resolves THIS stage - it must not short-circuit the rest of the
        // workflow. Exactly like a normal Approve, if later stages remain (evaluated against the
        // requisition's original EstimatedCost, never recalculated from the approved quantity alone),
        // the next one starts and the requisition stays UnderReview; the requisition is only finalized
        // once no stage remains, and then as PartiallyApproved rather than Approved because a decline
        // happened somewhere in this process (see ApprovalWorkflowEngine.AdvanceToNextActionableStageAsync).
        var pendingNotifications = await _engine.AdvanceAfterApprovalAsync(approval, userId, actorName, actorRole, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("This approval was already acted on. Please refresh.");
        }

        // Feature 8: anchored to the requisition itself, see SendBackApprovalCommandHandler's remarks.
        await _auditLogger.LogAsync("ApprovalPartiallyApproved", nameof(Requisition), requisition.Id,
            $"StageOrder={approval.StageOrder}; Comment={request.Comment}", cancellationToken);

        // Feature 9 (US-029/US-028): whatever AdvanceAfterApprovalAsync queued - the next stage's
        // approver(s), or a completion notice to the requestor if this was the final stage.
        foreach (var notification in pendingNotifications)
        {
            await _notificationService.NotifyAsync(notification, cancellationToken);
        }
    }
}
