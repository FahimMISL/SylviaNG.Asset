using MediatR;
using RMS.Application.Common;
using RMS.Application.Features.AuditLogs.DTOs;

namespace RMS.Application.Features.AuditLogs.Queries.GetAuditLog;

/// <summary>US-026. Every filter is optional - an all-null query returns the whole (company-scoped)
/// audit trail, newest first. Feature 11: real server-side pagination (Page/PageSize) - previously
/// this returned the entire filtered set and the frontend paginated it client-side.</summary>
public record GetAuditLogQuery(
    Guid? RequisitionId,
    DateTime? DateFrom,
    DateTime? DateTo,
    string? ActorSearch,
    string? ActionType,
    Guid? CategoryId,
    string? Department,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<AuditLogEntryDto>>;
