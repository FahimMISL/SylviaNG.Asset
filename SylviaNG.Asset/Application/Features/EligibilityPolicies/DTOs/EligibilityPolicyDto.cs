using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.EligibilityPolicies.DTOs;

public record EligibilityPolicyCriterionInput(EligibilityCriterionType CriterionType, string AllowedValue);

public record EligibilityPolicyCriterionDto(Guid Id, string CriterionType, string AllowedValue)
{
    public static EligibilityPolicyCriterionDto FromEntity(EligibilityPolicyCriterion c) => new(c.Id, c.CriterionType.ToString(), c.AllowedValue);
}

public record EligibilityPolicyReplacementRuleInput(int DurationValue, EligibilityDurationUnit DurationUnit);

public record EligibilityPolicyReplacementRuleDto(int DurationValue, string DurationUnit)
{
    public static EligibilityPolicyReplacementRuleDto FromEntity(EligibilityPolicyReplacementRule r) => new(r.DurationValue, r.DurationUnit.ToString());
}

public record EligibilityPolicyDto(
    Guid Id,
    string Name,
    string? Description,
    Guid CategoryId,
    string? CategoryName,
    Guid? CategoryItemId,
    string? CategoryItemName,
    bool IsActive,
    List<EligibilityPolicyCriterionDto> Criteria,
    EligibilityPolicyReplacementRuleDto? ReplacementRule)
{
    public static EligibilityPolicyDto FromEntity(EligibilityPolicy p) => new(
        p.Id, p.Name, p.Description, p.CategoryId, p.Category?.Name, p.CategoryItemId, p.CategoryItem?.Name, p.IsActive,
        p.Criteria.Select(EligibilityPolicyCriterionDto.FromEntity).ToList(),
        p.ReplacementRule is null ? null : EligibilityPolicyReplacementRuleDto.FromEntity(p.ReplacementRule));
}

/// <summary>List-row shape for the admin table - includes a short human-readable rule summary so the
/// list doesn't need a second round trip per row to explain what a policy does.</summary>
public record EligibilityPolicySummaryDto(
    Guid Id,
    string Name,
    string CategoryName,
    string? CategoryItemName,
    bool IsCategoryLevel,
    bool IsActive,
    string RuleSummary)
{
    public static EligibilityPolicySummaryDto FromEntity(EligibilityPolicy p) => new(
        p.Id, p.Name, p.Category?.Name ?? "(unknown category)", p.CategoryItem?.Name, p.CategoryItemId is null, p.IsActive,
        BuildRuleSummary(p));

    private static string BuildRuleSummary(EligibilityPolicy p)
    {
        var parts = new List<string>();

        var criteriaByType = p.Criteria.GroupBy(c => c.CriterionType);
        foreach (var group in criteriaByType)
        {
            parts.Add($"{group.Key}: {string.Join(" or ", group.Select(c => c.AllowedValue))}");
        }

        if (p.Criteria.Count == 0)
        {
            parts.Add("Open to everyone");
        }

        if (p.ReplacementRule is not null)
        {
            parts.Add($"Re-eligible every {p.ReplacementRule.DurationValue} {p.ReplacementRule.DurationUnit}");
        }

        return string.Join("; ", parts);
    }
}

/// <summary>GET api/eligibility-policies/check response - evaluated for the CURRENT authenticated
/// user (never a client-supplied user id), per PolicyEvaluationService.</summary>
public record EligibilityCheckResultDto(bool IsEligible, string? Reason, DateTime? NextEligibleDateUtc);
