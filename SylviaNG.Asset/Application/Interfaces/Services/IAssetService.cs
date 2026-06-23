using SylviaNG.Assets.Application.Features.Assets.Models;
using SylviaNG.Assets.SharedKernel.Pagination;

namespace SylviaNG.Assets.Application.Interfaces.Services
{
    public interface IAssetService
    {
        Task<long> CreateAsync(AssetCreateRequest request);
        Task UpdateAsync(long assetId, AssetUpdateRequest request);
        Task DeleteAsync(long assetId);
        Task<AssetResponse> GetByIdAsync(long assetId);
        Task<List<AssetResponse>> GetAllAsync();
        Task<PagedResult<AssetResponse>> GetPaginatedAsync(PagedRequest request);
        Task<List<AssetLookupResponse>> GetActiveBySiteIdAsync(long siteId);
    }
}