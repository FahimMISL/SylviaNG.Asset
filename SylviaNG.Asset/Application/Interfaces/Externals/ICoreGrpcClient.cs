using SylviaNG.Assets.Application.Common.Models;

namespace SylviaNG.Assets.Application.Interfaces.Externals
{
    public interface ICoreGrpcClient
    {
        Task<CoreBatchLookupResult> GetSitesAsync(List<long> siteIds);
    }
}
