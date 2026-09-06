using MediatR;
using Microsoft.EntityFrameworkCore;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Approvals.Services;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Approvals.Commands.PartialApproveApproval;

public class PartialApproveApprovalCommandHandler : IRequestHandler<PartialApproveApprovalCommand>
{
    private readonly IRequisitionApprovalRepository _requisitionApprovalRepository;
    private readonly IRequisitionRepository _requisitionRepository;
    private readonly IApprovalDelegationRepository _delegationRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public PartialApproveApprovalCommandHandler(
        IRequisitionApprovalRepository requisitionApprovalRepository, IRequisitionRepository requisitionRepository,
        IApprovalDelegationRepository delegationRepository, ICurrentUserService currentUser, IAuditLogger auditLogger, IUnitOfWork unitOfWork,
        INotificationService notificationService)
    {
        _requisitionApprovalRepository = requisitionApprovalRepository;
        _requisitionRepository = requisitionRepository;
        _delegationRepository = delegationRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
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
        var partialApproveEntry = requisition.PartialApprove(userId, actorName, actorRole, request.Comment);
        _requisitionRepository.AddStatusHistory(partialApproveEntry);

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

        // Feature 8: anchored to the requisition itself, see SendBackApprovalCommandHandler's remarks.
        await _auditLogger.LogAsync("ApprovalPartiallyApproved", nameof(Requisition), requisition.Id,
            $"StageOrder={approval.StageOrder}; Comment={request.Comment}", cancellationToken);

        await _notificationService.NotifyAsync(new NotificationRequest(
            requisition.CompanyId, requisition.RequestedByUserId, NotificationEventType.RequisitionPartiallyApproved, requisition.Id,
            new Dictionary<string, string>
            {
                ["RequisitionNumber"] = requisition.RequisitionNumber ?? "N/A",
                ["ActorName"] = actorName,
                ["ActorRole"] = actorRole ?? "N/A",
                ["Comment"] = request.Comment,
            }), cancellationToken);
    }
}
