using MediatR;
using RMS.Application.Features.Users.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Users.Commands.SetUserActiveState;

/// <summary>Backs both Activate and Deactivate - same shape as EligibilityPolicy's
/// SetEligibilityPolicyActiveStateCommand / ApprovalWorkflow's / RequisitionCategory's own
/// activate/deactivate commands. Deactivating a user takes effect immediately: a deactivated user is
/// excluded from ApprovalWorkflowEngine.ResolveApproversAsync's GetActiveByRoleAsync lookup, so they
/// stop being assignable as a Role-type approver on their very next resolution, with no workflow
/// edits required. Permission-guarded (Module=Rbac, Action=Edit).</summary>
public record SetUserActiveStateCommand(Guid UserId, bool IsActive) : IRequest<UserSummaryDto>, IPermissionGuardedRequest
{
    public PermissionModule Module => PermissionModule.Rbac;
    public PermissionAction Action => PermissionAction.Edit;
}
