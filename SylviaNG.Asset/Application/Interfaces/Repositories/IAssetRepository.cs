using SylviaNG.Assets.Domain.Entities;
using SylviaNG.Assets.SharedKernel.Generic;
using SylviaNG.Assets.SharedKernel.Pagination;

namespace SylviaNG.Assets.Application.Interfaces.Repositories
{
    public interface IAssetRepository : IRepository<Asset>
    {
        Task<Asset?> GetByAssetCodeAndSiteIdAsync(string assetCode, long siteId);
        Task<bool> ExistsByAssetCodeAndSiteIdAsync(string assetCode, long siteId, long? excludeId = null);
        Task<PagedResult<Asset>> GetPaginatedAsync(PagedRequest request);
        Task<List<Asset>> GetActiveBySiteIdAsync(long siteId);
    }
}