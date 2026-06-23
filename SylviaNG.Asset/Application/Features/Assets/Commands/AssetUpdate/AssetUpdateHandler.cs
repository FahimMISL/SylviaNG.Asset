using MediatR;
using SylviaNG.Assets.Application.Features.Assets.Models;
using SylviaNG.Assets.Application.Interfaces.Services;

namespace SylviaNG.Assets.Application.Features.Assets.Commands.AssetUpdate
{
    public class AssetUpdateHandler : IRequestHandler<AssetUpdateCommand, Unit>
    {
        private readonly IAssetService _assetService;

        public AssetUpdateHandler(IAssetService assetService)
        {
            _assetService = assetService;
        }

        public async Task<Unit> Handle(AssetUpdateCommand command, CancellationToken cancellationToken)
        {
            await _assetService.UpdateAsync(command.AssetId, command.Request);
            return Unit.Value;
        }
    }
}
