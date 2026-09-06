using MediatR;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Users.Commands.UpdateUserRole;

/// <summary>Feature 10 (US-031): the one new write capability this feature adds to the previously
/// read-only Users directory - reassigning who has which of the 7 fixed roles.</summary>
public record UpdateUserRoleCommand(Guid UserId, UserRole NewRole) : IRequest, IPermissionGuardedRequest
{
    public PermissionModule Module => PermissionModule.Rbac;
    public PermissionAction Action => PermissionAction.Edit;
}
