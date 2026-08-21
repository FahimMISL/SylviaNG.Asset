namespace RMS.Domain.Entities;

/// <summary>
/// Feature 5: line-item level, child of a RequisitionProcurementRecord - mirrors
/// PartialApprovalDecision's role for Feature 3. The two nullable fields are each set by exactly
/// one action type; which one is populated tells you which action this row belongs to.
/// </summary>
public class RequisitionProcurementLineItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RequisitionProcurementRecordId { get; set; }
    public RequisitionProcurementRecord? RequisitionProcurementRecord { get; set; }

    public Guid RequisitionItemId { get; set; }
    public RequisitionItem? RequisitionItem { get; set; }

    /// <summary>Set only on a StartProcessing record's line items - CategoryItem.Price snapshotted at
    /// the moment procurement began, so a later admin price edit doesn't retroactively change what
    /// this requisition was costed at.</summary>
    public decimal? UnitPrice { get; set; }

    /// <summary>Set only on a RecordFulfillment record's line items - the quantity fulfilled in THIS
    /// action specifically, not cumulative (RequisitionItem.FulfilledQuantity holds the running total).</summary>
    public int? QuantityFulfilledThisAction { get; set; }
}
