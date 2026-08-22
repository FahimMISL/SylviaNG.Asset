using RMS.Domain.Entities;

namespace RMS.Application.Interfaces;

public interface IAuditLogRepository
{
    /// <summary>
    /// Every filter is optional. Results are always scoped to companyId (Feature 8: an AuditLog row
    /// has no CompanyId of its own - it's derived from the audited entity, resolved per EntityName -
    /// so a SystemAdmin never sees another company's audit trail even though the table is shared).
    /// </summary>
    Task<List<AuditLog>> GetFilteredAsync(
        Guid companyId,
        Guid? requisitionId,
        DateTime? dateFrom,
        DateTime? dateTo,
        string? actorSearch,
        string? actionType,
        Guid? categoryId,
        string? department,
        CancellationToken cancellationToken = default);
}
