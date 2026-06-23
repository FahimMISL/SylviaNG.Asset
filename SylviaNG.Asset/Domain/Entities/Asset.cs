using SylviaNG.Assets.Domain.Enums;
using SylviaNG.Assets.SharedKernel.Audit;

namespace SylviaNG.Assets.Domain.Entities;

/// <summary>
/// Represents a physical company asset (equipment, furniture, vehicle, etc.).
/// </summary>
public class Asset : Audit
{
    public long AssetId { get; set; }
    public long SiteId { get; set; }
    public long? DepartmentId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AssetCategoryEnum Category { get; set; } = AssetCategoryEnum.General;
    public new AssetStatusEnum Status { get; set; } = AssetStatusEnum.Available;
    public long? AssignedToEmployeeId { get; set; }
    public decimal? PurchaseValue { get; set; }
    public DateTime? AcquiredDate { get; set; }
    public bool IsActive { get; set; } = true;
}