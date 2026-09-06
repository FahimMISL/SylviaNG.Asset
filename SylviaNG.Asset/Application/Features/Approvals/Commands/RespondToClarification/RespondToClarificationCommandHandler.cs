using MediatR;
using Microsoft.EntityFrameworkCore;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Approvals.Services;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Approvals.Commands.RespondToClarification;

/// <summary>Requestor-only (must be Requisition.RequestedByUserId - NOT an approver check, unlike
/// every other approval action). Resumes the stage and rolls the paused duration back into SlaDueUtc
/// so the approver doesn't lose the time they were waiting on the requestor.</summary>
public class RespondToClarificationCommandHandler : IRequestHandler<RespondToClarificationCommand>
{
    private readonly IRequisitionApprovalRepository _requisitionApprovalRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public RespondToClarificationCommandHandler(
        IRequisitionApprovalRepository requisitionApprovalRepository, ICurrentUserService currentUser,
        IAuditLogger auditLogger, IUnitOfWork unitOfWork)
    {
        _requisitionApprovalRepository = requisitionApprovalRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RespondToClarificationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();
        var actorName = _currentUser.FullName ?? "Unknown";
        var actorRole = _currentUser.Role?.ToString();

        var approval = await _requisitionApprovalRepository.GetApprovalByIdAsync(request.ApprovalId, cancellationToken)
            ?? throw new NotFoundException(nameof(RequisitionApproval), request.ApprovalId);

        var requisition = approval.RequisitionApprovalProcess!.Requisition!;
        if (requisition.RequestedByUserId != userId)
        {
            throw new ForbiddenException("Only the requestor can respond to a clarification request.");
        }

        if (approval.Status != RequisitionApprovalStatus.ClarificationRequested)
        {
            throw new ConflictException("This approval stage isn't waiting on a clarification response.");
        }

        if (approval.SlaPausedAtUtc.HasValue && approval.SlaDueUtc.HasValue)
        {
            var pausedDuration = DateTime.UtcNow - approval.SlaPausedAtUtc.Value;
            approval.SlaDueUtc = approval.SlaDueUtc.Value.Add(pausedDuration);
            approval.SlaPausedDurationTotal += pausedDuration;
        }
        approval.SlaPausedAtUtc = null;

        // Resume to whichever state the stage was actually in before the pause - derivable from
        // whether any parallel co-approver had already acted (no separate "prior status" field needed).
        approval.Status = approval.Assignments.Any(a => a.HasActed)
            ? RequisitionApprovalStatus.InProgress
            : RequisitionApprovalStatus.Pending;

        _requisitionApprovalRepository.AddAction(new RequisitionApprovalAction
        {
            RequisitionApprovalId = approval.Id,
            ActionType = ApprovalActionType.ClarificationResponse,
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
            throw new ConflictException("This approval was already updated. Please refresh.");
        }

        // Feature 8: anchored to the requisition itself, see SendBackApprovalCommandHandler's remarks.
        await _auditLogger.LogAsync("ApprovalClarificationResponded", nameof(Requisition), requisition.Id,
            $"Comment={request.Comment}", cancellationToken);
    }
}
