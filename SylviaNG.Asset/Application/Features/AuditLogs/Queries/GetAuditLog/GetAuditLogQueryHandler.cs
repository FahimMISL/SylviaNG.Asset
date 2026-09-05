using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Common;
using RMS.Application.Features.AuditLogs.DTOs;
using RMS.Application.Features.Requisitions.Services;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.AuditLogs.Queries.GetAuditLog;

public class GetAuditLogQueryHandler : IRequestHandler<GetAuditLogQuery, PagedResult<AuditLogEntryDto>>
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IRequisitionRepository _requisitionRepository;
    private readonly IApprovalDelegationRepository _delegationRepository;
    private readonly ICurrentUserService _currentUser;

    public GetAuditLogQueryHandler(
        IAuditLogRepository auditLogRepository, IRequisitionRepository requisitionRepository,
        IApprovalDelegationRepository delegationRepository, ICurrentUserService currentUser)
    {
        _auditLogRepository = auditLogRepository;
        _requisitionRepository = requisitionRepository;
        _delegationRepository = delegationRepository;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<AuditLogEntryDto>> Handle(GetAuditLogQuery request, CancellationToken cancellationToken)
    {
        // Feature 8.1: SystemAdmin keeps the unrestricted, cross-requisition console exactly as
        // before. Anyone else may only open ONE requisition's own audit trail (never the broad
        // query - RequisitionId is required), and only if they're already authorized to view that
        // requisition at all - reuses RequisitionAccessHelper, the same single-source-of-truth rule
        // the requisition detail page itself uses, so this never grants any access the user didn't
        // already effectively have.
        if (!_currentUser.IsInRole(UserRole.SystemAdmin))
        {
            var userId = _currentUser.UserId ?? throw new ForbiddenException();
            if (!request.RequisitionId.HasValue)
            {
                throw new ForbiddenException();
            }

            var requisition = await _requisitionRepository.GetByIdAsync(request.RequisitionId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(Requisition), request.RequisitionId.Value);

            var canAccess = await RequisitionAccessHelper.CanAccessAsync(
                requisition, userId, _currentUser, _delegationRepository, cancellationToken);
            if (!canAccess)
            {
                throw new ForbiddenException();
            }
        }

        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var result = await _auditLogRepository.GetFilteredAsync(
            companyId, request.RequisitionId, request.DateFrom, request.DateTo,
            request.ActorSearch, request.ActionType, request.CategoryId, request.Department,
            page, pageSize, request.SortDescending, cancellationToken);

        return new PagedResult<AuditLogEntryDto>(
            result.Items.Select(AuditLogEntryDto.FromEntity).ToList(), result.TotalCount, result.Page, result.PageSize);
    }
}
