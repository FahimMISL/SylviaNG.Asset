using Microsoft.EntityFrameworkCore;
using RMS.Application.Common;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Infrastructure.Data;

namespace RMS.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly RmsDbContext _context;

    public AuditLogRepository(RmsDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<AuditLog>> GetFilteredAsync(
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
        CancellationToken cancellationToken = default)
    {
        // Feature 8: AuditLog.CompanyId is captured directly at write time (AuditLogger.WriteAsync),
        // not resolved via a join to the audited entity - a row must stay visible/company-scoped even
        // after the entity it describes (e.g. a deleted requisition or category) no longer exists,
        // which is exactly what an append-only audit trail is for.
        var query = _context.AuditLogs.Where(a => a.CompanyId == companyId);

        if (requisitionId.HasValue)
        {
            query = query.Where(a => a.EntityName == nameof(Requisition) && a.EntityId == requisitionId.Value);
        }

        // categoryId/department are narrowing filters, not part of base company scoping, so they're
        // resolved via a join against the still-existing entity - a deleted requisition/category's
        // history simply won't match one of these two optional filters, which is an acceptable, narrow
        // limitation (the row is still visible in the unfiltered/requisitionId-filtered view).
        if (categoryId.HasValue)
        {
            var requisitionIdsInCategory = _context.Requisitions.Where(r => r.CategoryId == categoryId.Value).Select(r => r.Id);
            query = query.Where(a =>
                (a.EntityName == nameof(Requisition) && requisitionIdsInCategory.Contains(a.EntityId)) ||
                (a.EntityName == nameof(RequisitionCategory) && a.EntityId == categoryId.Value));
        }

        if (!string.IsNullOrWhiteSpace(department))
        {
            var requisitionIdsInDepartment = _context.Requisitions
                .Where(r => r.RequestedByUser != null && r.RequestedByUser.Department == department)
                .Select(r => r.Id);
            query = query.Where(a => a.EntityName == nameof(Requisition) && requisitionIdsInDepartment.Contains(a.EntityId));
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(a => a.TimestampUtc >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(a => a.TimestampUtc <= dateTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(actorSearch))
        {
            query = query.Where(a => EF.Functions.ILike(a.ActorName, $"%{actorSearch}%"));
        }

        if (!string.IsNullOrWhiteSpace(actionType))
        {
            query = query.Where(a => a.ActionType == actionType);
        }

        query = query.OrderByDescending(a => a.TimestampUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<AuditLog>(items, totalCount, page, pageSize);
    }
}
