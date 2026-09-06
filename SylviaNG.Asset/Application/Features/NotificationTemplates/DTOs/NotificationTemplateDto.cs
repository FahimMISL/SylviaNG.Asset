using RMS.Application.Features.Notifications.Services;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.NotificationTemplates.DTOs;

public record NotificationTemplateDto(
    NotificationEventType EventType,
    string EventLabel,
    string EmailSubject,
    string EmailBody,
    string InAppMessage,
    bool IsCustomized,
    DateTime? UpdatedAtUtc)
{
    public static NotificationTemplateDto FromCustom(NotificationTemplate entity) => new(
        entity.EventType, NotificationEventTypeCatalog.LabelOf(entity.EventType),
        entity.EmailSubject, entity.EmailBody, entity.InAppMessage, true, entity.UpdatedAtUtc);

    public static NotificationTemplateDto FromDefault(NotificationEventType eventType)
    {
        var content = DefaultNotificationTemplates.For(eventType);
        return new NotificationTemplateDto(
            eventType, NotificationEventTypeCatalog.LabelOf(eventType),
            content.EmailSubject, content.EmailBody, content.InAppMessage, false, null);
    }
}
