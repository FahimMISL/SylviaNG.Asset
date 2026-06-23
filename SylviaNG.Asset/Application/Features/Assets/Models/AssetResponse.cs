using SylviaNG.Assets.Domain.Enums;

namespace SylviaNG.Assets.Application.Features.Assets.Models
{
    public class AssetResponse
    {
        public long AssetId { get; set; }
        public long SiteId { get; set; }
        public string? SiteName { get; set; }
        public long? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public AssetCategoryEnum Category { get; set; }
        public AssetStatusEnum Status { get; set; }
        public long? AssignedToEmployeeId { get; set; }
        public decimal? PurchaseValue { get; set; }
        public DateTime? AcquiredDate { get; set; }
        public bool IsActive { get; set; }
    }
}