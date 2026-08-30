using MediatR;
using RMS.Application.Features.EligibilityPolicies.DTOs;

namespace RMS.Application.Features.EligibilityPolicies.Commands.CreateEligibilityPolicy;

/// <summary>Creates a policy with its nested criteria and optional replacement rule in one call,
/// mirroring how CreateApprovalWorkflowCommand accepts nested stages.</summary>
public record CreateEligibilityPolicyCommand(
    string Name,
    string? Description,
    Guid CategoryId,
    Guid? CategoryItemId,
    bool IsActive,
    List<EligibilityPolicyCriterionInput> Criteria,
    EligibilityPolicyReplacementRuleInput? ReplacementRule) : IRequest<EligibilityPolicyDto>;
