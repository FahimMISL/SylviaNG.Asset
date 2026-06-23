using SylviaNG.Assets.Domain.Enums;

namespace SylviaNG.Assets.Application.Features.Assets.Models
{
    public class AssetCreateRequest
    {
        public long SiteId { get; set; }
        public long? DepartmentId { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public AssetCategoryEnum Category { get; set; } = AssetCategoryEnum.General;
        public decimal? PurchaseValue { get; set; }
        public DateTime? AcquiredDate { get; set; }
    }
}