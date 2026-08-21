using MediatR;
using RMS.Application.Features.Reporting.DTOs;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Reporting.Queries.GetOperationalReport;

/// <summary>US-022. Every filter is optional - an all-null query returns every non-Draft requisition
/// for the current company.</summary>
public record GetOperationalReportQuery(
    DateTime? DateFrom,
    DateTime? DateTo,
    string? Department,
    Guid? CategoryId,
    Guid? CategoryItemId,
    List<RequisitionStatus>? Statuses,
    RequisitionPriority? Priority,
    string? RequesterSearch) : IRequest<List<OperationalReportRowDto>>;
