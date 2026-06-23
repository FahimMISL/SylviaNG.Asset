using Microsoft.EntityFrameworkCore;
using SylviaNG.Assets.Application.Interfaces.Repositories;
using SylviaNG.Assets.Domain.Entities;
using SylviaNG.Assets.Infrastructure.Data;
using SylviaNG.Assets.SharedKernel.Generic;
using SylviaNG.Assets.SharedKernel.Pagination;

namespace SylviaNG.Assets.Infrastructure.Repositories
{
    public class AssetRepository : Repository<Asset>, IAssetRepository
    {
        public AssetRepository(ApplicationDBContext dbContext) : base(dbContext) { }

        public async Task<Asset?> GetByAssetCodeAndSiteIdAsync(string assetCode, long siteId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(a => a.AssetCode == assetCode && a.SiteId == siteId);
        }

        public async Task<bool> ExistsByAssetCodeAndSiteIdAsync(string assetCode, long siteId, long? excludeId = null)
        {
            return await _dbSet
                .AnyAsync(a => a.AssetCode == assetCode && a.SiteId == siteId && (!excludeId.HasValue || a.AssetId != excludeId.Value));
        }

        public async Task<PagedResult<Asset>> GetPaginatedAsync(PagedRequest request)
        {
            var query = _dbSet.AsQueryable();
            return await query.ToPaginatedResultAsync(request);
        }

        public async Task<List<Asset>> GetActiveBySiteIdAsync(long siteId)
        {
            return await _dbSet
                .Where(a => a.SiteId == siteId && a.IsActive)
                .ToListAsync();
        }
    }
}