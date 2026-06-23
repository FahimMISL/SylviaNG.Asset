using MediatR;
using SylviaNG.Assets.Application.Features.Assets.Models;
using SylviaNG.Assets.Application.Interfaces.Services;

namespace SylviaNG.Assets.Application.Features.Assets.Queries.AssetGetById
{
    public class AssetGetByIdHandler : IRequestHandler<AssetGetByIdQuery, AssetResponse>
    {
        private readonly IAssetService _assetService;

        public AssetGetByIdHandler(IAssetService assetService)
        {
            _assetService = assetService;
        }

        public async Task<AssetResponse> Handle(AssetGetByIdQuery query, CancellationToken cancellationToken)
        {
            return await _assetService.GetByIdAsync(query.AssetId);
        }
    }
}
