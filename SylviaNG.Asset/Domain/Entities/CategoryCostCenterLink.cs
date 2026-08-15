namespace RMS.Domain.Entities;

/// <summary>Join entity: which cost centers are selectable for a category, per US-003 AC1.</summary>
public class CategoryCostCenterLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public RequisitionCategory? Category { get; set; }
    public Guid CostCenterId { get; set; }
    public CostCenter? CostCenter { get; set; }
}
