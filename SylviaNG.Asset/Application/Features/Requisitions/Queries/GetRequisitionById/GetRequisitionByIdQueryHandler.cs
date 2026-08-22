using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Approvals.Services;
using RMS.Application.Features.Requisitions.DTOs;
using RMS.Application.Features.Requisitions.Services;
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

        // Feature 3/5/10: owner / approver (delegation-aware) / procurement-in-pipeline /
        // department-head-same-department / SystemAdmin - see RequisitionAccessHelper's remarks.
        // Read-only for everyone but the requestor: approvers/procurement/department-head still cannot
        // edit requisition fields through this query, UpdateRequisitionCommand remains requestor-only.
        var canAccess = await RequisitionAccessHelper.CanAccessAsync(requisition, userId, _currentUser, _delegationRepository, cancellationToken);
        if (!canAccess)
        {
            throw new ForbiddenException();
        }

        // The one authoritative "should the frontend show approve/reject/etc. buttons" signal - same
        // delegation-aware effective-assignee resolution the real action handlers use to authorize,
        // so the frontend never has to (and can't get it wrong for an active out-of-office delegate
        // viewing the detail page directly rather than via the inbox).
        var currentUserCanAct = await ApprovalAuthorizationHelper.IsCurrentUserActionableAsync(
            requisition.ApprovalProcess, userId, _delegationRepository, cancellationToken);

        // Feature 5's equivalent authoritative signal - a static role check is enough here (unlike
        // approval's dynamic assignment-based check) since rule 1 makes every Procurement Officer
        // able to act on every requisition in the pipeline, with no per-user assignment to resolve.
        var currentUserCanProcess = (_currentUser.IsInRole(UserRole.SystemAdmin) || _currentUser.IsInRole(UserRole.ProcurementOfficer))
            && RequisitionAccessHelper.ProcurementPipelineStatuses.Contains(requisition.Status);

        return RequisitionDto.FromEntity(requisition, currentUserCanAct, currentUserCanProcess);
    }
}
