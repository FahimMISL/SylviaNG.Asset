using MediatR;
using RMS.Application.Features.Requisitions.DTOs;

namespace RMS.Application.Features.Requisitions.Queries.GetDepartmentRequisitions;

/// <summary>Feature 10 (US-032): DepartmentHead's own data-scoping list - mirrors GetMyRequisitionsQuery
/// exactly, scoped to the caller's own Department instead of their own UserId.</summary>
public record GetDepartmentRequisitionsQuery : IRequest<List<RequisitionSummaryDto>>;
