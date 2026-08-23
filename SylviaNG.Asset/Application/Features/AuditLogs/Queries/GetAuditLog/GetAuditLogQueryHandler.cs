using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Common;
using RMS.Application.Features.AuditLogs.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.AuditLogs.Queries.GetAuditLog;

public class GetAuditLogQueryHandler : IRequestHandler<GetAuditLogQuery, PagedResult<AuditLogEntryDto>>
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ICurrentUserService _currentUser;

    public GetAuditLogQueryHandler(IAuditLogRepository auditLogRepository, ICurrentUserService currentUser)
    {
        _auditLogRepository = auditLogRepository;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<AuditLogEntryDto>> Handle(GetAuditLogQuery request, CancellationToken cancellationToken)
    {
        // Feature 8: SystemAdmin only - by construction, no employee ever sees anyone's audit
        // history, their own included, matching the "no employee access to others' audit history"
        // requirement without needing a per-row ownership check.
        if (!_currentUser.IsInRole(UserRole.SystemAdmin))
        {
            throw new ForbiddenException();
        }

        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var result = await _auditLogRepository.GetFilteredAsync(
            companyId, request.RequisitionId, request.DateFrom, request.DateTo,
            request.ActorSearch, request.ActionType, request.CategoryId, request.Department,
            page, pageSize, cancellationToken);

        return new PagedResult<AuditLogEntryDto>(
            result.Items.Select(AuditLogEntryDto.FromEntity).ToList(), result.TotalCount, result.Page, result.PageSize);
    }
}
