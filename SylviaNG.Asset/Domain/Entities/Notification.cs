using RMS.Domain.Enums;

namespace RMS.Domain.Entities;

/// <summary>
/// Feature 9: one row per delivered in-app notification. Subject/Message are already-rendered text
/// (the template's merge tags are substituted once, at send time) - not template source, so a later
/// template edit never rewrites history a user already saw.
/// </summary>
public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid RecipientUserId { get; set; }
    public NotificationEventType EventType { get; set; }

    /// <summary>Nullable - SlaReminder/SlaBreachEscalated are the only events not always anchored to
    /// a single requisition today, though in practice they are too; kept nullable for safety.</summary>
    public Guid? RequisitionId { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    /// <summary>US-028/US-030: Rejected and Fulfilled are critical - never suppressed by a user
    /// preference, computed once at send time from NotificationEventTypeCatalog and stored so the
    /// inbox UI can show/pin it without re-deriving the rule.</summary>
    public bool IsCritical { get; set; }

    public bool IsRead { get; set; }
    public DateTime? ReadAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
