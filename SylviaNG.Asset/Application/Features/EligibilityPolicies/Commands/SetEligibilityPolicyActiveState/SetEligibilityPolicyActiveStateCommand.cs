using MediatR;
using RMS.Application.Features.EligibilityPolicies.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.EligibilityPolicies.Commands.SetEligibilityPolicyActiveState;

/// <summary>Backs both Activate and Deactivate - same shape as ApprovalWorkflow's
/// SetApprovalWorkflowActiveStateCommand / RequisitionCategory's SetCategoryActiveStateCommand.
/// Feature 10: permission-guarded (Module=EligibilityPolicy, Action=Edit).</summary>
public record SetEligibilityPolicyActiveStateCommand(Guid PolicyId, bool IsActive) : IRequest<EligibilityPolicyDto>, IPermissionGuardedRequest
{
    public PermissionModule Module => PermissionModule.EligibilityPolicy;
    public PermissionAction Action => PermissionAction.Edit;
}
