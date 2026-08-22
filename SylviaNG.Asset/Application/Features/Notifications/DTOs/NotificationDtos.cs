using RMS.Domain.Enums;

namespace RMS.Application.Features.Notifications.DTOs;

public record NotificationEntryDto(
    Guid Id,
    string EventType,
    string EventLabel,
    Guid? RequisitionId,
    string Subject,
    string Message,
    bool IsCritical,
    bool IsRead,
    DateTime? ReadAtUtc,
    DateTime CreatedAtUtc)
{
    public static NotificationEntryDto FromEntity(Domain.Entities.Notification entity) => new(
        entity.Id,
        entity.EventType.ToString(),
        NotificationEventTypeCatalog.LabelOf(entity.EventType),
        entity.RequisitionId,
        entity.Subject,
        entity.Message,
        entity.IsCritical,
        entity.IsRead,
        entity.ReadAtUtc,
        entity.CreatedAtUtc);
}

public record NotificationPageDto(List<NotificationEntryDto> Items, int TotalCount);

public record NotificationPreferenceDto(NotificationEventType EventType, string EventLabel, bool IsCritical, bool IsEnabled);
