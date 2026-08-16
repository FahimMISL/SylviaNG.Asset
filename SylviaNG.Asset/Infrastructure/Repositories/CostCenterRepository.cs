using Microsoft.EntityFrameworkCore;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Infrastructure.Data;

namespace RMS.Infrastructure.Repositories;

public class CostCenterRepository : ICostCenterRepository
{
    private readonly RmsDbContext _context;

    public CostCenterRepository(RmsDbContext context)
    {
        _context = context;
    }

    public Task<CostCenter?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.CostCenters.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<List<CostCenter>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) =>
        _context.CostCenters.Where(c => ids.Contains(c.Id)).ToListAsync(cancellationToken);

    public Task<List<CostCenter>> GetAllAsync(Guid companyId, bool? isActive, CancellationToken cancellationToken = default)
    {
        var query = _context.CostCenters.Where(c => c.CompanyId == companyId);
        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        return query.OrderBy(c => c.Code).ToListAsync(cancellationToken);
    }

    public Task<bool> CodeExistsAsync(Guid companyId, string code, Guid? excludeId, CancellationToken cancellationToken = default) =>
        _context.CostCenters.AnyAsync(
            c => c.CompanyId == companyId && c.Code == code && (excludeId == null || c.Id != excludeId),
            cancellationToken);

    public void Add(CostCenter costCenter) => _context.CostCenters.Add(costCenter);

    public void Delete(CostCenter costCenter) => _context.CostCenters.Remove(costCenter);

    public async Task<bool> IsInUseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (await _context.CategoryCostCenterLinks.AnyAsync(l => l.CostCenterId == id, cancellationToken))
        {
            return true;
        }
        return await _context.Requisitions.AnyAsync(r => r.CostCenterId == id, cancellationToken);
    }
}
