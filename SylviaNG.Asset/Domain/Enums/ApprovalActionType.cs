namespace RMS.Domain.Enums;

/// <summary>Every granular event recorded on the RequisitionApprovalAction ledger.</summary>
public enum ApprovalActionType
{
    Approve = 0,
    Reject = 1,
    SendBack = 2,
    RequestClarification = 3,
    ClarificationResponse = 4,
    Delegate = 5,
    Escalate = 6,
    PartialApprove = 7,
    AutoSkip = 8,
    SlaBreachEscalation = 9,
}
