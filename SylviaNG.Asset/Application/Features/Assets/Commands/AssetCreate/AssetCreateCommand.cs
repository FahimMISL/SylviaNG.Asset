using MediatR;
using SylviaNG.Assets.Application.Features.Assets.Models;

namespace SylviaNG.Assets.Application.Features.Assets.Commands.AssetCreate
{
    public class AssetCreateCommand : IRequest<long>
    {
        public AssetCreateRequest Request { get; set; }

        public AssetCreateCommand(AssetCreateRequest request)
        {
            Request = request;
        }
    }
}
