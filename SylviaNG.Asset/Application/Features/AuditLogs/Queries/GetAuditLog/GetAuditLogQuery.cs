using MediatR;
using RMS.Application.Features.AuditLogs.DTOs;

namespace RMS.Application.Features.AuditLogs.Queries.GetAuditLog;

/// <summary>US-026. Every filter is optional - an all-null query returns the whole (company-scoped)
/// audit trail, newest first.</summary>
public record GetAuditLogQuery(
    Guid? RequisitionId,
    DateTime? DateFrom,
    DateTime? DateTo,
    string? ActorSearch,
    string? ActionType,
    Guid? CategoryId,
    string? Department) : IRequest<List<AuditLogEntryDto>>;
