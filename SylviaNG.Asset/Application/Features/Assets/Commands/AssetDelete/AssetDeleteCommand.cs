using MediatR;

namespace SylviaNG.Assets.Application.Features.Assets.Commands.AssetDelete
{
    public class AssetDeleteCommand : IRequest<Unit>
    {
        public long AssetId { get; set; }

        public AssetDeleteCommand(long assetId)
        {
            AssetId = assetId;
        }
    }
}
