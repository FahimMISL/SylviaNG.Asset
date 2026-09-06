using RMS.Application.Common;
using RMS.Domain.Entities;

namespace RMS.Application.Interfaces;

public interface IAuditLogRepository
{
    /// <summary>
    /// Every filter is optional. Results are always scoped to companyId (Feature 8: an AuditLog row
    /// has no CompanyId of its own - it's derived from the audited entity, resolved per EntityName -
    /// so a SystemAdmin never sees another company's audit trail even though the table is shared).
    /// Feature 11: real server-side pagination added (previously returned the entire filtered set,
    /// paginated only in the browser) - pass a very large pageSize (see AuditLogController's export
    /// action) to get everything in one page, same query/filters, guaranteeing export rows match
    /// on-screen rows exactly as before.
    /// </summary>
    Task<PagedResult<AuditLog>> GetFilteredAsync(
        Guid companyId,
        Guid? requisitionId,
        DateTime? dateFrom,
        DateTime? dateTo,
        string? actorSearch,
        string? actionType,
        Guid? categoryId,
        string? department,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
