namespace RMS.Domain.Enums;

/// <summary>Status of one per-stage approval instance (RequisitionApproval).</summary>
public enum RequisitionApprovalStatus
{
    Pending = 0,
    InProgress = 1,
    Approved = 2,
    Rejected = 3,
    SentBack = 4,
    Skipped = 5,
    ClarificationRequested = 6,
}
