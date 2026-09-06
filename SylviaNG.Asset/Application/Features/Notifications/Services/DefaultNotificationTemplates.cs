using RMS.Domain.Enums;

namespace RMS.Application.Features.Notifications.Services;

public record DefaultTemplateContent(string EmailSubject, string EmailBody, string InAppMessage);

/// <summary>
/// Feature 9 (US-058): the built-in text used whenever no NotificationTemplate row exists for a
/// (CompanyId, EventType) - what "Reset to Default" actually resets to. Supported merge tags, used
/// across these defaults (a given event only fills in the ones relevant to it - see each hook site):
/// {{RequisitionNumber}} {{Category}} {{Item}} {{Quantity}} {{Status}} {{ActorName}} {{ActorRole}}
/// {{Comment}} {{Link}} {{Requestor}} {{Department}} {{Priority}} {{RequiredDate}}.
/// </summary>
public static class DefaultNotificationTemplates
{
    private static readonly Dictionary<NotificationEventType, DefaultTemplateContent> Defaults = new()
    {
        [NotificationEventType.RequisitionSubmitted] = new(
            "Requisition {{RequisitionNumber}} submitted",
            "Your requisition {{RequisitionNumber}} ({{Category}} - {{Item}}) was submitted successfully.\n\nView it here: {{Link}}",
            "Requisition {{RequisitionNumber}} submitted."),

        [NotificationEventType.ApprovalQueueEntry] = new(
            "Approval needed: {{RequisitionNumber}}",
            "A requisition needs your approval.\n\nRequestor: {{Requestor}}\nDepartment: {{Department}}\nRequisition: {{RequisitionNumber}}\nCategory: {{Category}}\nItem: {{Item}}\nQuantity: {{Quantity}}\nPriority: {{Priority}}\nRequired by: {{RequiredDate}}\n\nReview it here: {{Link}}",
            "{{Requestor}} needs your approval on {{RequisitionNumber}}."),

        [NotificationEventType.RequisitionApproved] = new(
            "Requisition {{RequisitionNumber}} approved",
            "Your requisition {{RequisitionNumber}} was fully approved by {{ActorName}} ({{ActorRole}}).\n\nView it here: {{Link}}",
            "Requisition {{RequisitionNumber}} approved."),

        [NotificationEventType.RequisitionRejected] = new(
            "Requisition {{RequisitionNumber}} rejected",
            "Your requisition {{RequisitionNumber}} was rejected by {{ActorName}} ({{ActorRole}}).\n\nReason: {{Comment}}\n\nView it here: {{Link}}",
            "Requisition {{RequisitionNumber}} was rejected."),

        [NotificationEventType.RequisitionSentBack] = new(
            "Requisition {{RequisitionNumber}} sent back",
            "Your requisition {{RequisitionNumber}} was sent back by {{ActorName}} ({{ActorRole}}) for changes.\n\nComment: {{Comment}}\n\nAmend and resubmit here: {{Link}}",
            "Requisition {{RequisitionNumber}} sent back for changes."),

        [NotificationEventType.ClarificationRequested] = new(
            "Clarification requested on {{RequisitionNumber}}",
            "{{ActorName}} ({{ActorRole}}) requested clarification on your requisition {{RequisitionNumber}}.\n\nComment: {{Comment}}\n\nRespond here: {{Link}}",
            "Clarification requested on {{RequisitionNumber}}."),

        [NotificationEventType.RequisitionPartiallyApproved] = new(
            "Requisition {{RequisitionNumber}} partially approved",
            "Your requisition {{RequisitionNumber}} was partially approved by {{ActorName}} ({{ActorRole}}).\n\nComment: {{Comment}}\n\nView it here: {{Link}}",
            "Requisition {{RequisitionNumber}} partially approved."),

        [NotificationEventType.ProcurementStarted] = new(
            "Procurement started for {{RequisitionNumber}}",
            "Procurement processing has started for your requisition {{RequisitionNumber}}.\n\nView it here: {{Link}}",
            "Procurement started for {{RequisitionNumber}}."),

        [NotificationEventType.RequisitionPartiallyFulfilled] = new(
            "Requisition {{RequisitionNumber}} partially fulfilled",
            "Part of your requisition {{RequisitionNumber}} has been delivered.\n\nView details here: {{Link}}",
            "Requisition {{RequisitionNumber}} partially fulfilled."),

        [NotificationEventType.RequisitionFulfilled] = new(
            "Requisition {{RequisitionNumber}} fulfilled",
            "Your requisition {{RequisitionNumber}} has been fully delivered.\n\nView details here: {{Link}}",
            "Requisition {{RequisitionNumber}} fulfilled."),

        [NotificationEventType.SlaBreachEscalated] = new(
            "Approval escalated to you (SLA breach): {{RequisitionNumber}}",
            "An approval on requisition {{RequisitionNumber}} breached its SLA and was escalated to you.\n\nReview it here: {{Link}}",
            "Approval on {{RequisitionNumber}} escalated to you (SLA breach)."),

        [NotificationEventType.SlaReminder] = new(
            "Approval SLA reminder: {{RequisitionNumber}}",
            "A pending approval assigned to you on requisition {{RequisitionNumber}} is approaching its SLA due date.\n\nReview it here: {{Link}}",
            "SLA reminder: approval pending on {{RequisitionNumber}}."),
    };

    public static DefaultTemplateContent For(NotificationEventType eventType) => Defaults[eventType];
}
