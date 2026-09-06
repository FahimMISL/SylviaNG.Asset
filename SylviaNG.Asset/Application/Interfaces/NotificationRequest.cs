using RMS.Domain.Enums;

namespace RMS.Application.Interfaces;

/// <summary>
/// Feature 9: everything NotificationService needs to render a template and persist a Notification -
/// CompanyId is passed explicitly rather than resolved from ICurrentUserService because the SLA
/// background service (Infrastructure/Services/SlaBreachEscalationService.cs) calls this from a
/// non-HTTP scope where no ambient current-user context exists.
/// </summary>
public record NotificationRequest(
    Guid CompanyId,
    Guid RecipientUserId,
    NotificationEventType EventType,
    Guid? RequisitionId,
    IReadOnlyDictionary<string, string> MergeTags);
