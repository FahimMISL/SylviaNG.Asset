using MediatR;
using SylviaNG.Assets.Application.Features.Assets.Models;
using SylviaNG.Assets.Application.Interfaces.Services;

namespace SylviaNG.Assets.Application.Features.Assets.Queries.AssetGetAll
{
    public class AssetGetAllHandler : IRequestHandler<AssetGetAllQuery, List<AssetResponse>>
    {
        private readonly IAssetService _assetService;

        public AssetGetAllHandler(IAssetService assetService)
        {
            _assetService = assetService;
        }

        public async Task<List<AssetResponse>> Handle(AssetGetAllQuery query, CancellationToken cancellationToken)
        {
            return await _assetService.GetAllAsync();
        }
    }
}
