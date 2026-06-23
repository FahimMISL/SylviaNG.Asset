using SylviaNG.Assets.Application.Common.Exceptions;
using SylviaNG.Assets.Application.Features.Assets.Models;
using SylviaNG.Assets.Application.Interfaces.Repositories;
using SylviaNG.Assets.Application.Interfaces.Services;
using SylviaNG.Assets.Application.Mappings;
using SylviaNG.Assets.SharedKernel.Generic;
using SylviaNG.Assets.SharedKernel.Pagination;

namespace SylviaNG.Assets.Application.Services
{
    public class AssetService : IAssetService
    {
        private readonly IAssetRepository _assetRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AssetService(
            IAssetRepository assetRepository,
            IUnitOfWork unitOfWork)
        {
            _assetRepository = assetRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<long> CreateAsync(AssetCreateRequest request)
        {
            var exists = await _assetRepository.ExistsByAssetCodeAndSiteIdAsync(request.AssetCode, request.SiteId);
            if (exists)
                throw new DuplicateException("Asset", "AssetCode", request.AssetCode);

            var entity = request.ToEntity();
            await _assetRepository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return entity.AssetId;
        }

        public async Task UpdateAsync(long assetId, AssetUpdateRequest request)
        {
            var entity = await _assetRepository.GetByIdAsync(assetId)
                ?? throw new NotFoundException("Asset", assetId);

            entity.ApplyUpdate(request);
            _assetRepository.Update(entity);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteAsync(long assetId)
        {
            var entity = await _assetRepository.GetByIdAsync(assetId)
                ?? throw new NotFoundException("Asset", assetId);

            _assetRepository.Delete(entity);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<AssetResponse> GetByIdAsync(long assetId)
        {
            var entity = await _assetRepository.GetByIdAsync(assetId)
                ?? throw new NotFoundException("Asset", assetId);

            return entity.ToResponse();
        }

        public async Task<List<AssetResponse>> GetAllAsync()
        {
            var entities = await _assetRepository.GetAllAsync();
            return entities.Select(e => e.ToResponse()).ToList();
        }

        public async Task<PagedResult<AssetResponse>> GetPaginatedAsync(PagedRequest request)
        {
            var pagedResult = await _assetRepository.GetPaginatedAsync(request);

            return new PagedResult<AssetResponse>
            {
                Data = pagedResult.Data.Select(e => e.ToResponse()).ToList(),
                TotalCount = pagedResult.TotalCount,
                PageNumber = pagedResult.PageNumber,
                PageSize = pagedResult.PageSize
            };
        }

        public async Task<List<AssetLookupResponse>> GetActiveBySiteIdAsync(long siteId)
        {
            var entities = await _assetRepository.GetActiveBySiteIdAsync(siteId);
            return entities.Select(e => e.ToLookupResponse()).ToList();
        }
    }
}