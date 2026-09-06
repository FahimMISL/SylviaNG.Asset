using Microsoft.EntityFrameworkCore;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Infrastructure.Data;

namespace RMS.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly RmsDbContext _context;

    public NotificationRepository(RmsDbContext context)
    {
        _context = context;
    }

    public async Task<(List<Notification> Items, int TotalCount)> GetForUserAsync(
        Guid userId, bool unreadOnly, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications.Where(n => n.RecipientUserId == userId);
        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.Notifications.CountAsync(n => n.RecipientUserId == userId && !n.IsRead, cancellationToken);

    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Notifications.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public Task<List<Notification>> GetUnreadForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.Notifications.Where(n => n.RecipientUserId == userId && !n.IsRead).ToListAsync(cancellationToken);
}
