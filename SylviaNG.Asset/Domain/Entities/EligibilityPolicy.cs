using RMS.Domain.Common;

namespace RMS.Domain.Entities;

/// <summary>
/// Feature 4 (Eligibility & Policy Management): per-company rule set that gates whether an employee
/// may request a given Category (or a specific CategoryItem within it). Mirrors ApprovalWorkflow's
/// shape - aggregate root, unique-active-per-scope enforced in the application layer (see
/// CreateEligibilityPolicyCommandHandler/UpdateEligibilityPolicyCommandHandler), Activate/Deactivate
/// toggle.
///
/// CategoryItemId null = this policy applies at the category level (any item under the category
/// without its own more specific policy); CategoryItemId set = this policy applies only to that one
/// item/type, and takes precedence over a category-level policy when both exist - see
/// PolicyEvaluationService's resolution order.
/// </summary>
public class EligibilityPolicy : AuditableEntity
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Guid CategoryId { get; set; }
    public RequisitionCategory? Category { get; set; }

    public Guid? CategoryItemId { get; set; }
    public CategoryItem? CategoryItem { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Soft-delete (Trash). A policy with IsDeleted=true is excluded from every normal
    /// list/detail/resolution query - see EligibilityPolicyRepository - but its row (and its Criteria/
    /// ReplacementRule children) stay in the database, recoverable via Restore() until
    /// EligibilityPolicyTrashPurgeService permanently removes it after the retention window.</summary>
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }

    public List<EligibilityPolicyCriterion> Criteria { get; set; } = new();

    /// <summary>0..1 - presence of this row means a replacement/waiting-period restriction is
    /// enabled for this policy; absence means no restriction at all.</summary>
    public EligibilityPolicyReplacementRule? ReplacementRule { get; set; }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void Delete(Guid? actorUserId)
    {
        IsDeleted = true;
        DeletedAtUtc = DateTime.UtcNow;
        DeletedByUserId = actorUserId;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAtUtc = null;
        DeletedByUserId = null;
    }
}
