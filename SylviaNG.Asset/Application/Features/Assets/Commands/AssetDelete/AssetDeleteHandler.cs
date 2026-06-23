using MediatR;
using SylviaNG.Assets.Application.Interfaces.Services;

namespace SylviaNG.Assets.Application.Features.Assets.Commands.AssetDelete
{
    public class AssetDeleteHandler : IRequestHandler<AssetDeleteCommand, Unit>
    {
        private readonly IAssetService _assetService;

        public AssetDeleteHandler(IAssetService assetService)
        {
            _assetService = assetService;
        }

        public async Task<Unit> Handle(AssetDeleteCommand command, CancellationToken cancellationToken)
        {
            await _assetService.DeleteAsync(command.AssetId);
            return Unit.Value;
        }
    }
}
