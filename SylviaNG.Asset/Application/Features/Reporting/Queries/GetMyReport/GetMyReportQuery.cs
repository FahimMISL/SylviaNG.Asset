using MediatR;
using RMS.Application.Features.Reporting.DTOs;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Reporting.Queries.GetMyReport;

/// <summary>The current user's own requisition history - every role has one, no restriction, since
/// it's inherently self-scoped (see GetMyReportQueryHandler's use of GetAllForUserAsync).</summary>
public record GetMyReportQuery(ReportPeriod Period) : IRequest<PersonReportDto>;
