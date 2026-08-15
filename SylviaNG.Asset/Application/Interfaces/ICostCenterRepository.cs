using RMS.Domain.Entities;

namespace RMS.Application.Interfaces;

public interface ICostCenterRepository
{
    Task<CostCenter?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<CostCenter>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<List<CostCenter>> GetAllAsync(Guid companyId, bool? isActive, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(Guid companyId, string code, Guid? excludeId, CancellationToken cancellationToken = default);
    void Add(CostCenter costCenter);
    void Delete(CostCenter costCenter);
    Task<bool> IsInUseAsync(Guid id, CancellationToken cancellationToken = default);
}
