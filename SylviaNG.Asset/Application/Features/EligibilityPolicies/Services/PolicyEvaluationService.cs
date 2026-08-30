using RMS.Application.Features.EligibilityPolicies.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.EligibilityPolicies.Services;

/// <summary>
/// Feature 4: resolves and evaluates the applicable EligibilityPolicy for a (user, category, item)
/// combination. Structural template: ApprovalWorkflowEngine (Application/Features/ApprovalWorkflows/
/// Services/ApprovalWorkflowEngine.cs) - same "resolve the one applicable configured thing, then
/// evaluate it" shape, same DI/registration convention (see Application's DependencyInjection).
///
/// Resolution precedence (IEligibilityPolicyRepository.GetResolvablePolicyAsync): the single active
/// policy scoped to (CategoryId, CategoryItemId) if one exists, else the single active policy scoped
/// to (CategoryId, CategoryItemId=null), else no restriction at all. At most one ACTIVE policy can
/// ever exist per exact scope (enforced by Create/UpdateEligibilityPolicyCommandHandler), so this
/// never has to arbitrate between two equally-applicable policies the way that would otherwise repeat
/// Feature 3's real "two active workflows both matched" bug.
/// </summary>
public class PolicyEvaluationService
{
    private readonly IEligibilityPolicyRepository _policyRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRequisitionRepository _requisitionRepository;

    /// <summary>A prior requisition only counts toward the replacement restriction while its CURRENT
    /// status is one of these - i.e. it was actually approved at some point and hasn't since moved
    /// somewhere that undoes that (Rejected/Cancelled/Draft/SentBack/Submitted/UnderReview never
    /// count, not even as a "no prior request" fallback - see RequisitionStatusRules for the full
    /// transition graph this list was checked against).</summary>
    private static readonly List<RequisitionStatus> QualifyingReplacementStatuses =
    [
        RequisitionStatus.Approved,
        RequisitionStatus.PartiallyApproved,
        RequisitionStatus.InProcurement,
        RequisitionStatus.Fulfilled,
        RequisitionStatus.PartiallyFulfilled,
        RequisitionStatus.Closed,
    ];

    public PolicyEvaluationService(
        IEligibilityPolicyRepository policyRepository, IUserRepository userRepository, IRequisitionRepository requisitionRepository)
    {
        _policyRepository = policyRepository;
        _userRepository = userRepository;
        _requisitionRepository = requisitionRepository;
    }

    /// <summary>Backend-authoritative eligibility check: always evaluated against the authenticated
    /// user's OWN DB record (userId here must come from ICurrentUserService, never a client-supplied
    /// attribute set) - never trust client-submitted Grade/Designation/etc.</summary>
    public async Task<EligibilityCheckResultDto> CheckAsync(Guid userId, Guid categoryId, Guid? categoryItemId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return new EligibilityCheckResultDto(false, "Your user record could not be found.", null);
        }

        var policy = await _policyRepository.GetResolvablePolicyAsync(user.CompanyId, categoryId, categoryItemId, cancellationToken);
        if (policy is null)
        {
            // No policy configured for this category/item at all - open access, no restriction.
            return new EligibilityCheckResultDto(true, null, null);
        }

        var criteriaFailure = EvaluateCriteria(policy, user);
        if (criteriaFailure is not null)
        {
            return new EligibilityCheckResultDto(false, criteriaFailure, null);
        }

        if (policy.ReplacementRule is null)
        {
            return new EligibilityCheckResultDto(true, null, null);
        }

        return await EvaluateReplacementRuleAsync(policy, userId, categoryId, categoryItemId, cancellationToken);
    }

    /// <summary>AND across different CriterionTypes, OR within the same CriterionType (see
    /// EligibilityPolicyCriterion's remarks). A type with zero criteria rows is open on that axis.
    /// Returns null when every axis passes, else a human-readable explanation of which attribute(s)
    /// didn't match.</summary>
    private static string? EvaluateCriteria(EligibilityPolicy policy, User user)
    {
        var failedAxes = new List<string>();

        foreach (var group in policy.Criteria.GroupBy(c => c.CriterionType))
        {
            var userValue = ResolveUserAttribute(user, group.Key);
            var allowedValues = group.Select(c => c.AllowedValue).ToList();

            var matches = userValue is not null && allowedValues.Any(v => string.Equals(v, userValue, StringComparison.OrdinalIgnoreCase));
            if (!matches)
            {
                var allowedText = string.Join(", ", allowedValues);
                var yourText = string.IsNullOrWhiteSpace(userValue) ? "not set" : userValue;
                failedAxes.Add($"{group.Key} must be one of: {allowedText} (your {group.Key}: {yourText})");
            }
        }

        if (failedAxes.Count == 0)
        {
            return null;
        }

        return $"You are not eligible for this item based on your profile - {string.Join("; ", failedAxes)}.";
    }

    private static string? ResolveUserAttribute(User user, EligibilityCriterionType type) => type switch
    {
        EligibilityCriterionType.Grade => user.Grade,
        EligibilityCriterionType.Designation => user.Designation,
        EligibilityCriterionType.EmploymentType => user.EmploymentType?.ToString(),
        EligibilityCriterionType.Department => user.Department,
        EligibilityCriterionType.Location => user.Location,
        _ => null,
    };

    /// <summary>Finds the user's most recent PRIOR requisition for this (CategoryId, CategoryItemId)
    /// that currently counts (see QualifyingReplacementStatuses), takes its Approved/PartiallyApproved
    /// transition timestamp, and blocks until DurationValue/DurationUnit later. No qualifying prior
    /// requisition at all (including "the only prior one was rejected/cancelled") = first-time
    /// request, allowed.</summary>
    private async Task<EligibilityCheckResultDto> EvaluateReplacementRuleAsync(
        EligibilityPolicy policy, Guid userId, Guid categoryId, Guid? categoryItemId, CancellationToken cancellationToken)
    {
        var lastApprovedUtc = await _requisitionRepository.GetMostRecentApprovedTransitionUtcAsync(
            userId, categoryId, categoryItemId, QualifyingReplacementStatuses, cancellationToken);

        if (lastApprovedUtc is null)
        {
            return new EligibilityCheckResultDto(true, null, null);
        }

        var rule = policy.ReplacementRule!;
        var nextEligibleDateUtc = AddDuration(lastApprovedUtc.Value, rule.DurationValue, rule.DurationUnit);

        if (DateTime.UtcNow < nextEligibleDateUtc)
        {
            var reason = $"You are not currently eligible for this item. You can request it again after {nextEligibleDateUtc:d MMMM yyyy}.";
            return new EligibilityCheckResultDto(false, reason, nextEligibleDateUtc);
        }

        return new EligibilityCheckResultDto(true, null, null);
    }

    private static DateTime AddDuration(DateTime start, int value, EligibilityDurationUnit unit) => unit switch
    {
        EligibilityDurationUnit.Days => start.AddDays(value),
        EligibilityDurationUnit.Months => start.AddMonths(value),
        EligibilityDurationUnit.Years => start.AddYears(value),
        _ => start.AddDays(value),
    };
}
