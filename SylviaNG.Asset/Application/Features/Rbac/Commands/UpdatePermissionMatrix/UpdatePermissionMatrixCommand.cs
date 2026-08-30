using MediatR;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Rbac.Commands.UpdatePermissionMatrix;

public record PermissionCellInput(PermissionModule Module, PermissionAction Action, bool IsAllowed);

public record UpdatePermissionMatrixCommand(UserRole Role, List<PermissionCellInput> Permissions) : IRequest, IPermissionGuardedRequest
{
    public PermissionModule Module => PermissionModule.Rbac;
    public PermissionAction Action => PermissionAction.Edit;
}
