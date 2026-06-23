using MediatR;
using SylviaNG.Assets.Application.Features.Assets.Models;

namespace SylviaNG.Assets.Application.Features.Assets.Commands.AssetUpdate
{
    public class AssetUpdateCommand : IRequest<Unit>
    {
        public long AssetId { get; set; }
        public AssetUpdateRequest Request { get; set; }

        public AssetUpdateCommand(long assetId, AssetUpdateRequest request)
        {
            AssetId = assetId;
            Request = request;
        }
    }
}
