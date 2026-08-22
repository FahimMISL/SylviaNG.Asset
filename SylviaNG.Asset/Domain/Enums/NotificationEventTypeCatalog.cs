namespace RMS.Domain.Enums;

/// <summary>
/// Feature 9: the one place IsCritical/label are decided per event type, so the send path, the
/// preferences UI, and the template UI can never disagree with each other about which events are
/// critical.
/// </summary>
public static class NotificationEventTypeCatalog
{
    private static readonly HashSet<NotificationEventType> CriticalTypes =
    [
        NotificationEventType.RequisitionRejected,
        NotificationEventType.RequisitionFulfilled,
    ];

    private static readonly Dictionary<NotificationEventType, string> Labels = new()
    {
        [NotificationEventType.RequisitionSubmitted] = "Requisition Submitted",
        [NotificationEventType.ApprovalQueueEntry] = "Approval Needed",
        [NotificationEventType.RequisitionApproved] = "Requisition Approved",
        [NotificationEventType.RequisitionRejected] = "Requisition Rejected",
        [NotificationEventType.RequisitionSentBack] = "Requisition Sent Back",
        [NotificationEventType.ClarificationRequested] = "Clarification Requested",
        [NotificationEventType.RequisitionPartiallyApproved] = "Requisition Partially Approved",
        [NotificationEventType.ProcurementStarted] = "Procurement Started",
        [NotificationEventType.RequisitionPartiallyFulfilled] = "Partially Fulfilled",
        [NotificationEventType.RequisitionFulfilled] = "Fulfilled",
        [NotificationEventType.SlaBreachEscalated] = "SLA Breach Escalated",
        [NotificationEventType.SlaReminder] = "SLA Reminder",
    };

    public static bool IsCritical(NotificationEventType eventType) => CriticalTypes.Contains(eventType);

    public static string LabelOf(NotificationEventType eventType) =>
        Labels.TryGetValue(eventType, out var label) ? label : eventType.ToString();

    public static IReadOnlyList<NotificationEventType> AllTypes { get; } =
        Enum.GetValues<NotificationEventType>();
}
