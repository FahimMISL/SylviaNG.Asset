namespace RMS.Application.Interfaces;

/// <summary>Centralized notification send path backing US-028/US-029/US-030(delivery). Checks the
/// recipient's preference (unless the event type is critical), renders the applicable template,
/// persists the in-app Notification row, and logs delivery via IAuditLogger.</summary>
public interface INotificationService
{
    Task NotifyAsync(NotificationRequest request, CancellationToken cancellationToken = default);
}
