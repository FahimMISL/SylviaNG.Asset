namespace RMS.Domain.Enums;

/// <summary>
/// Full lifecycle per FR-RR-006. Approval-stage transitions (Submitted through
/// PartiallyApproved) are only ever entered by Feature 3 (Approval Workflow) -
/// Feature 2 prepares the requisition up to Submitted/UnderReview and provides
/// the Cancel/SentBack-amendment paths that are its own responsibility.
/// </summary>
public enum RequisitionStatus
{
    Draft = 0,
    Submitted = 1,
    UnderReview = 2,
    Approved = 3,
    Rejected = 4,
    PartiallyApproved = 5,
    SentBack = 6,
    InProcurement = 7,
    Fulfilled = 8,
    PartiallyFulfilled = 9,
    Closed = 10,
    Cancelled = 11,
}
