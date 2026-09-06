using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Interfaces;

public interface INotificationPreferenceRepository
{
    Task<List<NotificationPreference>> GetAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>No row means enabled by default - see NotificationPreference's remarks.</summary>
    Task<bool> IsEnabledAsync(Guid userId, NotificationEventType eventType, CancellationToken cancellationToken = default);

    /// <summary>Creates or updates the row for (userId, eventType). Tracked only - caller saves.</summary>
    Task UpsertAsync(Guid userId, NotificationEventType eventType, bool isEnabled, CancellationToken cancellationToken = default);
}
