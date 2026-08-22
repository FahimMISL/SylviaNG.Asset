using MediatR;
using RMS.Application.Features.EligibilityPolicies.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.EligibilityPolicies.Commands.UpdateEligibilityPolicy;

/// <summary>Feature 10: permission-guarded (Module=EligibilityPolicy, Action=Edit).</summary>
public record UpdateEligibilityPolicyCommand(
    Guid PolicyId,
    string Name,
    string? Description,
    Guid CategoryId,
    Guid? CategoryItemId,
    bool IsActive,
    List<EligibilityPolicyCriterionInput> Criteria,
    EligibilityPolicyReplacementRuleInput? ReplacementRule) : IRequest<EligibilityPolicyDto>, IPermissionGuardedRequest
{
    public PermissionModule Module => PermissionModule.EligibilityPolicy;
    public PermissionAction Action => PermissionAction.Edit;
}
