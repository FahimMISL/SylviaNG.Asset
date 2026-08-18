namespace RMS.Domain.Entities;

/// <summary>Line-item level, child of a PartialApprove action. DeclineReason's "required when
/// DeclinedQuantity > 0" rule is enforced by the command validator, not a DB constraint - per spec
/// US-051, the minimum-order-quantity warning is dropped: no MOQ field exists anywhere on
/// RequisitionItem/CategoryItem/RequisitionCategory today, and this feature does not invent one.</summary>
public class PartialApprovalDecision
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RequisitionApprovalActionId { get; set; }
    public RequisitionApprovalAction? RequisitionApprovalAction { get; set; }

    public Guid RequisitionItemId { get; set; }
    public RequisitionItem? RequisitionItem { get; set; }

    public int ApprovedQuantity { get; set; }
    public int DeclinedQuantity { get; set; }
    public string? DeclineReason { get; set; }
}
