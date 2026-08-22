using RMS.Domain.Enums;

namespace RMS.Domain.Entities;

/// <summary>
/// Feature 9: per-user, per-event-type opt-out. No row for a given (UserId, EventType) means enabled
/// by default - so existing users need no backfill, and a row only ever exists to record a disable.
/// Critical event types (see NotificationEventTypeCatalog) are never written here; the send path
/// checks IsCritical before ever consulting this table, so a critical type can't be turned off no
/// matter what a client sends to the update endpoint.
/// </summary>
public class NotificationPreference
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public NotificationEventType EventType { get; set; }
    public bool IsEnabled { get; set; } = true;
}
