using MediatR;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.EligibilityPolicies.Commands.DeleteEligibilityPolicy;

/// <summary>Deletes freely - unlike an ApprovalWorkflow, a policy's past evaluations aren't tracked
/// anywhere (no audit-critical historical record references it), so there's nothing to check for
/// before removing it. Cascades to its Criteria and ReplacementRule. Feature 10: permission-guarded
/// (Module=EligibilityPolicy, Action=Delete).</summary>
public record DeleteEligibilityPolicyCommand(Guid PolicyId) : IRequest, IPermissionGuardedRequest
{
    public PermissionModule Module => PermissionModule.EligibilityPolicy;
    public PermissionAction Action => PermissionAction.Delete;
}
