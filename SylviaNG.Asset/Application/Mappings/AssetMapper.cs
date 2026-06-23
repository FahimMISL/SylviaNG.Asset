using SylviaNG.Assets.Application.Features.Assets.Models;
using SylviaNG.Assets.Domain.Entities;

namespace SylviaNG.Assets.Application.Mappings
{
    /// <summary>
    /// Manual mapping methods for the Asset entity.
    /// Follow this pattern for all new feature mappings.
    /// </summary>
    public static class AssetMapper
    {
        public static Asset ToEntity(this AssetCreateRequest request)
        {
            return new Asset
            {
                SiteId = request.SiteId,
                DepartmentId = request.DepartmentId,
                AssetCode = request.AssetCode,
                Name = request.Name,
                Description = request.Description,
                Category = request.Category,
                PurchaseValue = request.PurchaseValue,
                AcquiredDate = request.AcquiredDate,
                IsActive = true
            };
        }

        public static void ApplyUpdate(this Asset entity, AssetUpdateRequest request)
        {
            if (request.DepartmentId.HasValue) entity.DepartmentId = request.DepartmentId;
            if (request.AssetCode != null) entity.AssetCode = request.AssetCode;
            if (request.Name != null) entity.Name = request.Name;
            if (request.Description != null) entity.Description = request.Description;
            if (request.Category.HasValue) entity.Category = request.Category.Value;
            if (request.Status.HasValue) entity.Status = request.Status.Value;
            if (request.AssignedToEmployeeId.HasValue) entity.AssignedToEmployeeId = request.AssignedToEmployeeId;
            if (request.PurchaseValue.HasValue) entity.PurchaseValue = request.PurchaseValue;
            if (request.AcquiredDate.HasValue) entity.AcquiredDate = request.AcquiredDate;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
        }

        public static AssetResponse ToResponse(this Asset entity)
        {
            return new AssetResponse
            {
                AssetId = entity.AssetId,
                SiteId = entity.SiteId,
                DepartmentId = entity.DepartmentId,
                AssetCode = entity.AssetCode,
                Name = entity.Name,
                Description = entity.Description,
                Category = entity.Category,
                Status = entity.Status,
                AssignedToEmployeeId = entity.AssignedToEmployeeId,
                PurchaseValue = entity.PurchaseValue,
                AcquiredDate = entity.AcquiredDate,
                IsActive = entity.IsActive
            };
        }

        public static AssetLookupResponse ToLookupResponse(this Asset entity)
        {
            return new AssetLookupResponse
            {
                AssetId = entity.AssetId,
                AssetCode = entity.AssetCode,
                Name = entity.Name
            };
        }
    }
}