using MediatR;
using Microsoft.EntityFrameworkCore;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Approvals.Services;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Approvals.Commands.RequestClarification;

/// <summary>Pauses the SLA clock (SlaPausedAtUtc = now) while the ball is in the requestor's court -
/// the approver does NOT act yet, so their assignment's HasActed stays false.</summary>
public class RequestClarificationCommandHandler : IRequestHandler<RequestClarificationCommand>
{
    private readonly IRequisitionApprovalRepository _requisitionApprovalRepository;
    private readonly IApprovalDelegationRepository _delegationRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public RequestClarificationCommandHandler(
        IRequisitionApprovalRepository requisitionApprovalRepository, IApprovalDelegationRepository delegationRepository,
        ICurrentUserService currentUser, IAuditLogger auditLogger, IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _requisitionApprovalRepository = requisitionApprovalRepository;
        _delegationRepository = delegationRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task Handle(RequestClarificationCommand request, CancellationToken cancellationToken)
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

        // Authorization only - RequestClarification doesn't consume the assignment's action slot.
        await ApprovalAuthorizationHelper.GetActionableAssignmentAsync(approval, userId, _delegationRepository, cancellationToken);

        approval.Status = RequisitionApprovalStatus.ClarificationRequested;
        approval.SlaPausedAtUtc = DateTime.UtcNow;

        _requisitionApprovalRepository.AddAction(new RequisitionApprovalAction
        {
            RequisitionApprovalId = approval.Id,
            ActionType = ApprovalActionType.RequestClarification,
            ActorUserId = userId,
            ActorName = actorName,
            ActorRole = actorRole,
            Comment = request.Comment,
        });

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("This approval was already acted on. Please refresh.");
        }

        // Feature 8: anchored to the requisition itself, see SendBackApprovalCommandHandler's remarks.
        await _auditLogger.LogAsync("ApprovalClarificationRequested", nameof(Requisition), approval.RequisitionApprovalProcess!.RequisitionId,
            $"Comment={request.Comment}", cancellationToken);

        var requisition = approval.RequisitionApprovalProcess.Requisition!;
        await _notificationService.NotifyAsync(new NotificationRequest(
            requisition.CompanyId, requisition.RequestedByUserId, NotificationEventType.ClarificationRequested, requisition.Id,
            new Dictionary<string, string>
            {
                ["RequisitionNumber"] = requisition.RequisitionNumber ?? "N/A",
                ["ActorName"] = actorName,
                ["ActorRole"] = actorRole ?? "N/A",
                ["Comment"] = request.Comment,
            }), cancellationToken);
    }
}
