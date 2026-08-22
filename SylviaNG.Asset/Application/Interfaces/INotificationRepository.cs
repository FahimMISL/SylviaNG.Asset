using RMS.Domain.Entities;

namespace RMS.Application.Interfaces;

public interface INotificationRepository
{
    Task<(List<Notification> Items, int TotalCount)> GetForUserAsync(
        Guid userId, bool unreadOnly, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Notification>> GetUnreadForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
