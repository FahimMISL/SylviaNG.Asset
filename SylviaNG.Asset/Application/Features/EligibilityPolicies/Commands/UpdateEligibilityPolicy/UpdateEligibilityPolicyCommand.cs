using MediatR;
using RMS.Application.Features.EligibilityPolicies.DTOs;

namespace RMS.Application.Features.EligibilityPolicies.Commands.UpdateEligibilityPolicy;

public record UpdateEligibilityPolicyCommand(
    Guid PolicyId,
    string Name,
    string? Description,
    Guid CategoryId,
    Guid? CategoryItemId,
    bool IsActive,
    List<EligibilityPolicyCriterionInput> Criteria,
    EligibilityPolicyReplacementRuleInput? ReplacementRule) : IRequest<EligibilityPolicyDto>;
