using MediatR;
using Microsoft.EntityFrameworkCore;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Approvals.Services;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Approvals.Commands.DelegateApprovalAction;

public class DelegateApprovalActionCommandHandler : IRequestHandler<DelegateApprovalActionCommand>
{
    private readonly IRequisitionApprovalRepository _requisitionApprovalRepository;
    private readonly IApprovalDelegationRepository _delegationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public DelegateApprovalActionCommandHandler(
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

    public async Task Handle(DelegateApprovalActionCommand request, CancellationToken cancellationToken)
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

        var delegateUser = await _userRepository.GetByIdAsync(request.DelegateToUserId, cancellationToken);
        if (delegateUser is null || !delegateUser.IsActive)
        {
            throw new NotFoundException(nameof(User), request.DelegateToUserId);
        }

        // Directly mutates AssignedUserId, keeping OriginalApproverUserId for provenance - permanent,
        // no "period" to revert (unlike ApprovalDelegation), per the plan.
        var originalAssignee = assignment.AssignedUserId;
        assignment.AssignedUserId = request.DelegateToUserId;
        assignment.OriginalApproverUserId ??= originalAssignee;

        _requisitionApprovalRepository.AddAction(new RequisitionApprovalAction
        {
            RequisitionApprovalId = approval.Id,
            ActionType = ApprovalActionType.Delegate,
            ActorUserId = userId,
            ActorName = actorName,
            ActorRole = actorRole,
            Comment = request.Comment,
            DelegatedToUserId = request.DelegateToUserId,
        });

        if (approval.Status == RequisitionApprovalStatus.Pending)
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

        await _auditLogger.LogAsync("ApprovalDelegated", nameof(RequisitionApproval), approval.Id,
            $"RequisitionId={approval.RequisitionApprovalProcess!.RequisitionId}, DelegatedTo={request.DelegateToUserId}", cancellationToken);
    }
}
