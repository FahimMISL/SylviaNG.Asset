using MediatR;
using Microsoft.EntityFrameworkCore;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Approvals.Services;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Approvals.Commands.SendBackApproval;

/// <summary>SendBack: UnderReview -> SentBack. On a later Resubmit (existing, unchanged) a fresh
/// RequisitionApproval instance is created for the same StageOrder by ApprovalWorkflowEngine the next
/// time the requisition is submitted - prior instances (including this one) stay in history.</summary>
public class SendBackApprovalCommandHandler : IRequestHandler<SendBackApprovalCommand>
{
    private readonly IRequisitionApprovalRepository _requisitionApprovalRepository;
    private readonly IRequisitionRepository _requisitionRepository;
    private readonly IApprovalDelegationRepository _delegationRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public SendBackApprovalCommandHandler(
        IRequisitionApprovalRepository requisitionApprovalRepository, IRequisitionRepository requisitionRepository,
        IApprovalDelegationRepository delegationRepository, ICurrentUserService currentUser, IAuditLogger auditLogger, IUnitOfWork unitOfWork)
    {
        _requisitionApprovalRepository = requisitionApprovalRepository;
        _requisitionRepository = requisitionRepository;
        _delegationRepository = delegationRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SendBackApprovalCommand request, CancellationToken cancellationToken)
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

        assignment.HasActed = true;
        assignment.ActedAtUtc = DateTime.UtcNow;
        approval.Status = RequisitionApprovalStatus.SentBack;

        _requisitionApprovalRepository.AddAction(new RequisitionApprovalAction
        {
            RequisitionApprovalId = approval.Id,
            ActionType = ApprovalActionType.SendBack,
            ActorUserId = userId,
            ActorName = actorName,
            ActorRole = actorRole,
            Comment = request.Comment,
        });

        var requisition = approval.RequisitionApprovalProcess!.Requisition!;
        var sendBackEntry = requisition.SendBack(userId, actorName, actorRole, request.Comment);
        _requisitionRepository.AddStatusHistory(sendBackEntry);

        approval.RequisitionApprovalProcess.CurrentStageOrder = null;

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("This approval was already acted on. Please refresh.");
        }

        // Feature 8: anchored to the requisition itself (not the stage instance) so "view audit
        // history for one requisition" actually finds every approval decision made on it.
        await _auditLogger.LogAsync("ApprovalSentBack", nameof(Requisition), requisition.Id,
            $"StageOrder={approval.StageOrder}; Comment={request.Comment}", cancellationToken);
    }
}
