using MediatR;
using RMS.Application.Features.Reporting.DTOs;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Reporting.Queries.GetAllUsersReport;

/// <summary>SystemAdmin/Ceo see the whole company's people; DepartmentHead sees only their own
/// department's people (see GetAllUsersReportQueryHandler) - matches the same data-isolation boundary
/// GetDepartmentRequisitionsQueryHandler already enforces elsewhere. Every other role is forbidden.</summary>
public record GetAllUsersReportQuery(ReportPeriod Period) : IRequest<AllUsersReportDto>;
