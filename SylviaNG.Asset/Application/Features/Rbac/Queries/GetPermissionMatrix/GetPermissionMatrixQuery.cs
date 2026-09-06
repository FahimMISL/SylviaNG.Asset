using MediatR;
using RMS.Application.Features.Rbac.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Rbac.Queries.GetPermissionMatrix;

public record GetPermissionMatrixQuery(UserRole Role) : IRequest<PermissionMatrixDto>, IPermissionGuardedRequest
{
    public PermissionModule Module => PermissionModule.Rbac;
    public PermissionAction Action => PermissionAction.View;
}
