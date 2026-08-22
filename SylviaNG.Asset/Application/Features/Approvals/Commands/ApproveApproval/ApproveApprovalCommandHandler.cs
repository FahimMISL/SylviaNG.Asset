using MediatR;
using Microsoft.EntityFrameworkCore;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.ApprovalWorkflows.Services;
using RMS.Application.Features.Approvals.Services;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Approvals.Commands.ApproveApproval;

public class ApproveApprovalCommandHandler : IRequestHandler<ApproveApprovalCommand>
{
    private readonly IRequisitionApprovalRepository _requisitionApprovalRepository;
    private readonly IApprovalDelegationRepository _delegationRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApprovalWorkflowEngine _engine;

    public ApproveApprovalCommandHandler(
        IRequisitionApprovalRepository requisitionApprovalRepository, IApprovalDelegationRepository delegationRepository,
        ICurrentUserService currentUser, IAuditLogger auditLogger, IUnitOfWork unitOfWork, ApprovalWorkflowEngine engine)
    {
        _requisitionApprovalRepository = requisitionApprovalRepository;
        _delegationRepository = delegationRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
        _engine = engine;
    }

    public async Task Handle(ApproveApprovalCommand request, CancellationToken cancellationToken)
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

        // Not required even on a CapturesEstimatedCost stage - there's no Procurement Officer
        // feature yet to be the real source of a cost figure, so forcing the approver to guess
        // one would be fake data. If it's left blank, AdvanceAfterApprovalAsync simply leaves
        // Requisition.EstimatedCost untouched, which means any cost-based conditional stage
        // downstream won't match (safe default: no known cost = no cost-triggered escalation).
        var stageCapturesEstimatedCost = approval.ApprovalWorkflowStage!.CapturesEstimatedCost;

        assignment.HasActed = true;
        assignment.ActedAtUtc = DateTime.UtcNow;

        // First responder wins on a Role-type approver fanned out to several users - once this one
        // acts, close out the rest of the same RoleFanoutGroupId so they stop showing as pending
        // elsewhere and don't block stage completion. No-op for any other assignment.
        ApprovalWorkflowEngine.CloseRoleFanoutSiblings(approval, assignment);

        _requisitionApprovalRepository.AddAction(new RequisitionApprovalAction
        {
            RequisitionApprovalId = approval.Id,
            ActionType = ApprovalActionType.Approve,
            ActorUserId = userId,
            ActorName = actorName,
            ActorRole = actorRole,
            Comment = request.Comment,
            CapturedEstimatedCost = stageCapturesEstimatedCost ? request.EstimatedCost : null,
        });

        var stageComplete = ApprovalWorkflowEngine.IsStageComplete(approval);
        if (stageComplete)
        {
            approval.Status = RequisitionApprovalStatus.Approved;
            await _engine.AdvanceAfterApprovalAsync(approval, userId, actorName, actorRole, cancellationToken);
        }
        else if (approval.Status == RequisitionApprovalStatus.Pending)
        {
            approval.Status = RequisitionApprovalStatus.InProgress;
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("This approval was already acted on. Please refresh.");
        }

        // Feature 8: anchored to the requisition itself, see SendBackApprovalCommandHandler's remarks.
        await _auditLogger.LogAsync("ApprovalApproved", nameof(Requisition), approval.RequisitionApprovalProcess!.RequisitionId,
            $"StageOrder={approval.StageOrder}; Comment={request.Comment}", cancellationToken);
    }
}
