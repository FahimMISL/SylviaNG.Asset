using MediatR;
using RMS.Application.Features.Rbac.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Rbac.Queries.GetRoles;

/// <summary>Feature 10 (US-031). The 7 fixed roles this app has - see the plan's Context for why role
/// names aren't user-creatable with the current single-JWT-claim architecture.</summary>
public record GetRolesQuery : IRequest<List<RoleSummaryDto>>, IPermissionGuardedRequest
{
    public PermissionModule Module => PermissionModule.Rbac;
    public PermissionAction Action => PermissionAction.View;
}
