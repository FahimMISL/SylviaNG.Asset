using MediatR;
using RMS.Application.Features.EligibilityPolicies.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.EligibilityPolicies.Commands.CreateEligibilityPolicy;

/// <summary>Creates a policy with its nested criteria and optional replacement rule in one call,
/// mirroring how CreateApprovalWorkflowCommand accepts nested stages. Feature 10: permission-guarded
/// (Module=EligibilityPolicy, Action=Create) - the controller's [Authorize(Roles=SystemAdmin)] is
/// currently neutralized by the class-level [AllowAnonymous] dev stub, so this is the only real gate.</summary>
public record CreateEligibilityPolicyCommand(
    string Name,
    string? Description,
    Guid CategoryId,
    Guid? CategoryItemId,
    bool IsActive,
    List<EligibilityPolicyCriterionInput> Criteria,
    EligibilityPolicyReplacementRuleInput? ReplacementRule) : IRequest<EligibilityPolicyDto>, IPermissionGuardedRequest
{
    public PermissionModule Module => PermissionModule.EligibilityPolicy;
    public PermissionAction Action => PermissionAction.Create;
}
