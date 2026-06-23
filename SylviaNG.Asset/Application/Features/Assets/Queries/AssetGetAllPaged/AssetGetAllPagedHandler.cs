using MediatR;
using SylviaNG.Assets.Application.Features.Assets.Models;
using SylviaNG.Assets.Application.Interfaces.Services;
using SylviaNG.Assets.SharedKernel.Pagination;

namespace SylviaNG.Assets.Application.Features.Assets.Queries.AssetGetAllPaged
{
    public class AssetGetAllPagedHandler : IRequestHandler<AssetGetAllPagedQuery, PagedResult<AssetResponse>>
    {
        private readonly IAssetService _assetService;

        public AssetGetAllPagedHandler(IAssetService assetService)
        {
            _assetService = assetService;
        }

        public async Task<PagedResult<AssetResponse>> Handle(AssetGetAllPagedQuery query, CancellationToken cancellationToken)
        {
            return await _assetService.GetPaginatedAsync(query.Request);
        }
    }
}
