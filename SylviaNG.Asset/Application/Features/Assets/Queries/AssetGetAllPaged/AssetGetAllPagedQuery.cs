using MediatR;
using SylviaNG.Assets.Application.Features.Assets.Models;
using SylviaNG.Assets.SharedKernel.Pagination;

namespace SylviaNG.Assets.Application.Features.Assets.Queries.AssetGetAllPaged
{
    public class AssetGetAllPagedQuery : IRequest<PagedResult<AssetResponse>>
    {
        public PagedRequest Request { get; set; }

        public AssetGetAllPagedQuery(PagedRequest request)
        {
            Request = request;
        }
    }
}
