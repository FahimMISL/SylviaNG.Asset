using MediatR;
using Microsoft.EntityFrameworkCore;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Approvals.Services;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Approvals.Commands.EscalateApproval;

public class EscalateApprovalCommandHandler : IRequestHandler<EscalateApprovalCommand>
{
    private readonly IRequisitionApprovalRepository _requisitionApprovalRepository;
    private readonly IApprovalDelegationRepository _delegationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public EscalateApprovalCommandHandler(
        IRequisitionApprovalRepository requisitionApprovalRepository, IApprovalDelegationRepository delegationRepository,
        IUserRepository userRepository, ICurrentUserService currentUser, IAuditLogger auditLogger, IUnitOfWork unitOfWork)
    {
        _requisitionApprovalRepository = requisitionApprovalRepository;
        _delegationRepository = delegationRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(EscalateApprovalCommand request, CancellationToken cancellationToken)
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

        await ApprovalAuthorizationHelper.GetActionableAssignmentAsync(approval, userId, _delegationRepository, cancellationToken);

        var sla = approval.ApprovalWorkflowStage!.Sla
            ?? throw new ConflictException("This stage has no SLA/escalation target configured.");

        var companyId = approval.RequisitionApprovalProcess!.Requisition!.CompanyId;
        var targets = await ApprovalEscalationHelper.ResolveEscalationTargetsAsync(sla, companyId, _userRepository, cancellationToken);
        if (targets.Count == 0)
        {
            throw new ConflictException("This stage's configured escalation target has no active user available.");
        }

        ApprovalEscalationHelper.ApplyEscalation(
            _requisitionApprovalRepository, approval, targets, ApprovalActionType.Escalate, userId, actorName, actorRole, request.Comment);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("This approval was already acted on. Please refresh.");
        }

        // Feature 8: anchored to the requisition itself, see SendBackApprovalCommandHandler's remarks.
        await _auditLogger.LogAsync("ApprovalEscalated", nameof(Requisition), approval.RequisitionApprovalProcess.RequisitionId,
            $"Comment={request.Comment}", cancellationToken);
    }
}
