using MediatR;
using Microsoft.EntityFrameworkCore;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Approvals.Services;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Approvals.Commands.RejectApproval;

public class RejectApprovalCommandHandler : IRequestHandler<RejectApprovalCommand>
{
    private readonly IRequisitionApprovalRepository _requisitionApprovalRepository;
    private readonly IRequisitionRepository _requisitionRepository;
    private readonly IApprovalDelegationRepository _delegationRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public RejectApprovalCommandHandler(
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

    public async Task Handle(RejectApprovalCommand request, CancellationToken cancellationToken)
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

        // A single required rejection ends the requisition immediately - the approval flips straight to
        // Rejected, which implicitly "skips" every other sibling assignment: the pending-approvals query
        // filters on approval.Status in (Pending, InProgress), so once this leaves that set no other
        // assignee sees it anymore, with no separate per-assignment field to mutate.
        approval.Status = RequisitionApprovalStatus.Rejected;

        _requisitionApprovalRepository.AddAction(new RequisitionApprovalAction
        {
            RequisitionApprovalId = approval.Id,
            ActionType = ApprovalActionType.Reject,
            ActorUserId = userId,
            ActorName = actorName,
            ActorRole = actorRole,
            Comment = request.Comment,
        });

        var requisition = approval.RequisitionApprovalProcess!.Requisition!;
        var rejectEntry = requisition.Reject(userId, actorName, actorRole, request.Comment);
        _requisitionRepository.AddStatusHistory(rejectEntry);

        approval.RequisitionApprovalProcess.CurrentStageOrder = null;
        approval.RequisitionApprovalProcess.CompletedAtUtc = DateTime.UtcNow;

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("This approval was already acted on. Please refresh.");
        }

        await _auditLogger.LogAsync("ApprovalRejected", nameof(RequisitionApproval), approval.Id,
            $"RequisitionId={requisition.Id}, StageOrder={approval.StageOrder}", cancellationToken);
    }
}
