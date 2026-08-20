using RMS.Application.Features.EligibilityPolicies.DTOs;

namespace RMS.Api.Controllers.Requests;

/// <summary>Shared body shape for Create (Category/CategoryItemId come from this body) and
/// Update (PolicyId comes from the route, not this body) - same convention as
/// SaveApprovalWorkflowVersionRequestBody/UpdateApprovalWorkflowVersionRequestBody.</summary>
public record SaveEligibilityPolicyRequestBody(
    string Name,
    string? Description,
    Guid CategoryId,
    Guid? CategoryItemId,
    bool IsActive,
    List<EligibilityPolicyCriterionInput> Criteria,
    EligibilityPolicyReplacementRuleInput? ReplacementRule);
