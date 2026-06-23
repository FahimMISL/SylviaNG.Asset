using Grpc.Core;
using SylviaNG.Assets.Application.Common.Models;
using SylviaNG.Assets.Application.Interfaces.Externals;
using SylviaNG.Assets.Grpc.Generated.Core;

namespace SylviaNG.Assets.Infrastructure.Services
{
    public class CoreGrpcClient : ICoreGrpcClient
    {
        private readonly CoreService.CoreServiceClient _client;
        private readonly ILogger<CoreGrpcClient> _logger;

        public CoreGrpcClient(CoreService.CoreServiceClient client, ILogger<CoreGrpcClient> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<CoreBatchLookupResult> GetSitesAsync(List<long> siteIds)
        {
            try
            {
                var request = new BatchLookupRequest();
                request.SiteIds.AddRange(siteIds);

                var response = await _client.BatchLookupAsync(request);

                var result = new CoreBatchLookupResult
                {
                    Sites = response.Sites.Select(s => new EntityIdNameCodeResponse
                    {
                        EntityId = s.Id,
                        Name = s.Name,
                        Code = s.Code
                    }).ToList()
                };

                return result;
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "gRPC error calling CoreService.BatchLookup");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error calling CoreService.BatchLookup");
                throw;
            }
        }
    }
}
