using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Interfaces;

public interface INotificationTemplateRepository
{
    Task<NotificationTemplate?> GetByEventTypeAsync(Guid companyId, NotificationEventType eventType, CancellationToken cancellationToken = default);
    Task<List<NotificationTemplate>> GetAllAsync(Guid companyId, CancellationToken cancellationToken = default);
    void Add(NotificationTemplate template);
    void Remove(NotificationTemplate template);
}
