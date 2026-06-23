using SylviaNG.Assets.Domain.Enums;

namespace SylviaNG.Assets.Application.Features.Assets.Models
{
    public class AssetUpdateRequest
    {
        public long? DepartmentId { get; set; }
        public string? AssetCode { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public AssetCategoryEnum? Category { get; set; }
        public AssetStatusEnum? Status { get; set; }
        public long? AssignedToEmployeeId { get; set; }
        public decimal? PurchaseValue { get; set; }
        public DateTime? AcquiredDate { get; set; }
        public bool? IsActive { get; set; }
    }
}