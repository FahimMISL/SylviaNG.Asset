using RMS.Domain.Common;

namespace RMS.Domain.Entities;

/// <summary>One requested line item within a Requisition (US-004 "Add Another Item").</summary>
public class RequisitionItem : AuditableEntity
{
    public Guid RequisitionId { get; set; }
    public Requisition? Requisition { get; set; }

    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }

    /// <summary>Links back to the Admin-defined Item/Type this row resolved from. Nullable so
    /// deleting an Item/Type later doesn't break historical requisitions - ItemName stays intact.</summary>
    public Guid? CategoryItemId { get; set; }
    public CategoryItem? CategoryItem { get; set; }
}
