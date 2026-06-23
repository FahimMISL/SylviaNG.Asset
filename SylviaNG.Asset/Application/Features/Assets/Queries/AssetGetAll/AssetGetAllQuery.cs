using MediatR;
using SylviaNG.Assets.Application.Features.Assets.Models;

namespace SylviaNG.Assets.Application.Features.Assets.Queries.AssetGetAll
{
    public class AssetGetAllQuery : IRequest<List<AssetResponse>>
    {
    }
}
