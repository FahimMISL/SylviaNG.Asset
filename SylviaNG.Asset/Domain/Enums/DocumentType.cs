namespace RMS.Domain.Enums;

/// <summary>Feature 13: what kind of document a RequisitionAttachment is. SupportingDocument is the
/// default - it's what every attachment was, undifferentiated, before this feature. Deliberately no
/// Vendor Agreement/Quotation/RFQ/Comparison/Selection/Contract type - Vendor Management doesn't
/// exist in this system.</summary>
public enum DocumentType
{
    SupportingDocument = 0,
    ApprovalMemo = 1,
    JobDescription = 2,
    ProcurementDocument = 3,
    FulfillmentDocument = 4,
    Invoice = 5,
    Other = 6,
}
