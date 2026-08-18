namespace RMS.Application.Interfaces;

/// <summary>
/// Per spec section 29: the actual "send a notification" call is isolated behind this interface so a
/// real implementation (email/push/in-app) can be plugged in later (Feature 11) without touching any
/// approval-workflow code. No notification platform is built as part of Feature 3.
/// </summary>
public interface INotificationService
{
    Task NotifyAsync(Guid userId, string subject, string message, CancellationToken cancellationToken = default);
}
