namespace RMS.Domain.Enums;

/// <summary>
/// Feature 9 (US-028/US-029/US-030-delivery-only). The fixed set of events the Notification Center
/// actually sends - not every RequisitionStatus transition, only the ones the approved scope names.
/// </summary>
public enum NotificationEventType
{
    RequisitionSubmitted = 0,
    ApprovalQueueEntry = 1,
    RequisitionApproved = 2,
    RequisitionRejected = 3,
    RequisitionSentBack = 4,
    ClarificationRequested = 5,
    RequisitionPartiallyApproved = 6,
    ProcurementStarted = 7,
    RequisitionPartiallyFulfilled = 8,
    RequisitionFulfilled = 9,
    SlaBreachEscalated = 10,
    SlaReminder = 11,
}
