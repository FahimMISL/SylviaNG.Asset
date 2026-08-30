using Microsoft.EntityFrameworkCore;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Infrastructure.Data;

namespace RMS.Infrastructure.Repositories;

public class EligibilityPolicyRepository : IEligibilityPolicyRepository
{
    private readonly RmsDbContext _context;

    public EligibilityPolicyRepository(RmsDbContext context)
    {
        _context = context;
    }

    private IQueryable<EligibilityPolicy> QueryWithDetails() =>
        _context.EligibilityPolicies
            .Include(p => p.Category)
            .Include(p => p.CategoryItem)
            .Include(p => p.Criteria)
            .Include(p => p.ReplacementRule);

    public Task<EligibilityPolicy?> GetByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default) =>
        QueryWithDetails().FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Id == id, cancellationToken);

    public Task<List<EligibilityPolicy>> GetAllAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        QueryWithDetails().Where(p => p.CompanyId == companyId).OrderBy(p => p.Name).ToListAsync(cancellationToken);

    public Task<bool> ActivePolicyExistsAsync(
        Guid companyId, Guid categoryId, Guid? categoryItemId, Guid? excludeId, CancellationToken cancellationToken = default) =>
        _context.EligibilityPolicies.AnyAsync(
            p => p.CompanyId == companyId
                && p.CategoryId == categoryId
                && p.CategoryItemId == categoryItemId
                && p.IsActive
                && (excludeId == null || p.Id != excludeId),
            cancellationToken);

    public async Task<EligibilityPolicy?> GetResolvablePolicyAsync(
        Guid companyId, Guid categoryId, Guid? categoryItemId, CancellationToken cancellationToken = default)
    {
        if (categoryItemId.HasValue)
        {
            var itemPolicy = await QueryWithDetails().FirstOrDefaultAsync(
                p => p.CompanyId == companyId && p.CategoryId == categoryId && p.CategoryItemId == categoryItemId && p.IsActive,
                cancellationToken);
            if (itemPolicy is not null)
            {
                return itemPolicy;
            }
        }

        return await QueryWithDetails().FirstOrDefaultAsync(
            p => p.CompanyId == companyId && p.CategoryId == categoryId && p.CategoryItemId == null && p.IsActive,
            cancellationToken);
    }

    public void Add(EligibilityPolicy policy) => _context.EligibilityPolicies.Add(policy);

    public void ReplaceCriteria(EligibilityPolicy policy, List<EligibilityPolicyCriterion> newCriteria)
    {
        _context.EligibilityPolicyCriteria.RemoveRange(policy.Criteria);
        policy.Criteria.Clear();

        foreach (var criterion in newCriteria)
        {
            criterion.EligibilityPolicyId = policy.Id;
        }

        // Added directly to the DbSet (not via policy.Criteria.AddRange) so EF Core tracks these as
        // new inserts instead of mis-detecting them as updates - see
        // RequisitionRepository.ReplaceItems for the same fix applied here.
        _context.EligibilityPolicyCriteria.AddRange(newCriteria);
    }

    public void SetReplacementRule(EligibilityPolicy policy, EligibilityPolicyReplacementRule? newRule)
    {
        if (policy.ReplacementRule is not null)
        {
            _context.EligibilityPolicyReplacementRules.Remove(policy.ReplacementRule);
            policy.ReplacementRule = null;
        }

        if (newRule is not null)
        {
            newRule.EligibilityPolicyId = policy.Id;
            _context.EligibilityPolicyReplacementRules.Add(newRule);
            policy.ReplacementRule = newRule;
        }
    }

    public void Remove(EligibilityPolicy policy) => _context.EligibilityPolicies.Remove(policy);
}
