using MediatR;
using SylviaNG.Assets.Application.Features.Assets.Models;

namespace SylviaNG.Assets.Application.Features.Assets.Queries.AssetGetById
{
    public class AssetGetByIdQuery : IRequest<AssetResponse>
    {
        public long AssetId { get; set; }

        public AssetGetByIdQuery(long assetId)
        {
            AssetId = assetId;
        }
    }
}
