using RMS.Domain.Entities;

namespace RMS.Application.Interfaces;

public interface IEligibilityPolicyRepository
{
    /// <summary>Feature 10: scoped by CompanyId (like every other repository's GetByIdAsync) so a
    /// mismatched company's policy 404s instead of being readable by GUID alone - closes a real
    /// cross-tenant leak this method used to have.</summary>
    Task<EligibilityPolicy?> GetByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default);
    Task<List<EligibilityPolicy>> GetAllAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>At-most-one-active-per-scope guard used by Create/Update handlers (returns true if an
    /// ACTIVE policy already covers this exact (CompanyId, CategoryId, CategoryItemId) combination,
    /// excluding excludeId when updating that same policy) - the same class of two-active-things-
    /// resolve-ambiguously bug Feature 3 already had with overlapping "all categories" workflows.</summary>
    Task<bool> ActivePolicyExistsAsync(
        Guid companyId, Guid categoryId, Guid? categoryItemId, Guid? excludeId, CancellationToken cancellationToken = default);

    /// <summary>PolicyEvaluationService's resolution order: the single active policy scoped exactly to
    /// (CategoryId, CategoryItemId) if one exists, else the single active policy scoped to
    /// (CategoryId, CategoryItemId=null), else null (open/no restriction).</summary>
    Task<EligibilityPolicy?> GetResolvablePolicyAsync(
        Guid companyId, Guid categoryId, Guid? categoryItemId, CancellationToken cancellationToken = default);

    void Add(EligibilityPolicy policy);

    /// <summary>Wholesale-replaces a policy's criteria, deleting whatever isn't in the new set -
    /// same convention as RequisitionRepository.ReplaceItems.</summary>
    void ReplaceCriteria(EligibilityPolicy policy, List<EligibilityPolicyCriterion> newCriteria);

    /// <summary>Sets (or clears, when newRule is null) the 0..1 replacement rule child.</summary>
    void SetReplacementRule(EligibilityPolicy policy, EligibilityPolicyReplacementRule? newRule);

    void Remove(EligibilityPolicy policy);
}
