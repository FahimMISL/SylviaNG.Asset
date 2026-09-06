using MediatR;
using RMS.Application.Features.Dashboard.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Dashboard.Queries.GetManpowerSummary;

/// <summary>Feature 12 - HR Manager's dashboard widget. Company-wide across every department (unlike
/// Department Head's own department-only manpower requisitions), restricted to HrManager/SystemAdmin
/// only in the handler (Manpower/View alone isn't precise enough - Department Head also holds that
/// grant but must NOT get this company-wide read).</summary>
public record GetManpowerSummaryQuery : IRequest<ManpowerSummaryDto>, IPermissionGuardedRequest
{
    public PermissionModule Module => PermissionModule.Manpower;
    public PermissionAction Action => PermissionAction.View;
}
