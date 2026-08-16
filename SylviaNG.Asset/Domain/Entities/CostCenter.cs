using RMS.Domain.Common;

namespace RMS.Domain.Entities;

/// <summary>
/// Minimal internal cost center master per US-003 AC4. Manual CRUD only for
/// this slice; CSV import and live finance-system sync (AC5) are deferred -
/// CSV import is a fast-follow within Feature 1, API sync belongs to US-057.
/// </summary>
public class CostCenter : AuditableEntity
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
