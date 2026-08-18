using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Approvals.Services;
using RMS.Application.Features.Requisitions.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Requisitions.Queries.GetRequisitionById;

public class GetRequisitionByIdQueryHandler : IRequestHandler<GetRequisitionByIdQuery, RequisitionDto>
{
    private readonly IRequisitionRepository _requisitionRepository;
    private readonly IApprovalDelegationRepository _delegationRepository;
    private readonly ICurrentUserService _currentUser;

    public GetRequisitionByIdQueryHandler(
        IRequisitionRepository requisitionRepository, IApprovalDelegationRepository delegationRepository, ICurrentUserService currentUser)
    {
        _requisitionRepository = requisitionRepository;
        _delegationRepository = delegationRepository;
        _currentUser = currentUser;
    }

    public async Task<RequisitionDto> Handle(GetRequisitionByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();

        var requisition = await _requisitionRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Requisition), request.Id);

        // Feature 3: extended, not replaced - the original owner-only check stays as the (a) branch.
        // Read-only for everyone but the requestor: approvers still cannot edit requisition fields
        // through this query, UpdateRequisitionCommand remains requestor-only and untouched.
        var isOwner = requisition.RequestedByUserId == userId;
        var isSystemAdmin = _currentUser.IsInRole(UserRole.SystemAdmin);
        var isAuthorizedApprover = !isOwner && !isSystemAdmin && await HasEverHadAnAssignmentAsync(requisition, userId, cancellationToken);

        if (!isOwner && !isSystemAdmin && !isAuthorizedApprover)
        {
            throw new ForbiddenException();
        }

        // The one authoritative "should the frontend show approve/reject/etc. buttons" signal - same
        // delegation-aware effective-assignee resolution the real action handlers use to authorize,
        // so the frontend never has to (and can't get it wrong for an active out-of-office delegate
        // viewing the detail page directly rather than via the inbox).
        var currentUserCanAct = await ApprovalAuthorizationHelper.IsCurrentUserActionableAsync(
            requisition.ApprovalProcess, userId, _delegationRepository, cancellationToken);

        return RequisitionDto.FromEntity(requisition, currentUserCanAct);
    }

    /// <summary>(c) has ever had a RequisitionApprovalAssignment (effective, delegation-aware) on this
    /// requisition's approval process - either as the original assignee (who keeps read access even
    /// while an out-of-office delegation is active, since delegation never mutates the assignment row),
    /// or as someone currently standing in for one via an active ApprovalDelegation.</summary>
    private async Task<bool> HasEverHadAnAssignmentAsync(Requisition requisition, Guid userId, CancellationToken cancellationToken)
    {
        if (requisition.ApprovalProcess is null)
        {
            return false;
        }

        var assignedUserIds = requisition.ApprovalProcess.StageInstances
            .SelectMany(s => s.Assignments)
            .Select(a => a.AssignedUserId)
            .Distinct()
            .ToList();

        if (assignedUserIds.Contains(userId))
        {
            return true;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        foreach (var assignedUserId in assignedUserIds)
        {
            var effectiveAssignee = await ApprovalAuthorizationHelper.ResolveEffectiveAssigneeAsync(
                assignedUserId, _delegationRepository, today, cancellationToken);
            if (effectiveAssignee == userId)
            {
                return true;
            }
        }

        return false;
    }
}
