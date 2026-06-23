using MediatR;
using SylviaNG.Assets.Application.Features.Assets.Models;
using SylviaNG.Assets.Application.Interfaces.Services;

namespace SylviaNG.Assets.Application.Features.Assets.Commands.AssetCreate
{
    public class AssetCreateHandler : IRequestHandler<AssetCreateCommand, long>
    {
        private readonly IAssetService _assetService;

        public AssetCreateHandler(IAssetService assetService)
        {
            _assetService = assetService;
        }

        public async Task<long> Handle(AssetCreateCommand command, CancellationToken cancellationToken)
        {
            return await _assetService.CreateAsync(command.Request);
        }
    }
}
