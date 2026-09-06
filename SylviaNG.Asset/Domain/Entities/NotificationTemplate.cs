using RMS.Domain.Enums;

namespace RMS.Domain.Entities;

/// <summary>
/// Feature 9 (US-058). One optional customization row per (CompanyId, EventType). No SMS field - SMS
/// isn't configured anywhere in this project, so it's omitted rather than stubbed. Email is designed
/// for (Subject/Body carry merge tags) but actual SMTP delivery is a documented gap this pass - see
/// NotificationService's remarks. A row only exists once an admin customizes that event type; absent,
/// DefaultNotificationTemplates supplies the built-in text, which is what makes "Reset to Default"
/// just a delete.
/// </summary>
public class NotificationTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public NotificationEventType EventType { get; set; }

    public string EmailSubject { get; set; } = string.Empty;
    public string EmailBody { get; set; } = string.Empty;
    public string InAppMessage { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid UpdatedByUserId { get; set; }
}
