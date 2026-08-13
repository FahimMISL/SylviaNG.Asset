namespace RMS.Domain.Enums;

/// <summary>
/// Minimal lifecycle for this slice: a requisition is either an editable
/// Draft or has been Submitted. Approval/procurement/fulfillment statuses
/// belong to later features (US-006, Feature 3+) and are not modeled yet.
/// </summary>
public enum RequisitionStatus
{
    Draft = 0,
    Submitted = 1,
}
