using Microsoft.EntityFrameworkCore;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;
using RMS.Infrastructure.Data;

namespace RMS.Infrastructure.Repositories;

public class NotificationPreferenceRepository : INotificationPreferenceRepository
{
    private readonly RmsDbContext _context;

    public NotificationPreferenceRepository(RmsDbContext context)
    {
        _context = context;
    }

    public Task<List<NotificationPreference>> GetAllForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.NotificationPreferences.Where(p => p.UserId == userId).ToListAsync(cancellationToken);

    public async Task<bool> IsEnabledAsync(Guid userId, NotificationEventType eventType, CancellationToken cancellationToken = default)
    {
        var preference = await _context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.EventType == eventType, cancellationToken);
        return preference?.IsEnabled ?? true;
    }

    public async Task UpsertAsync(Guid userId, NotificationEventType eventType, bool isEnabled, CancellationToken cancellationToken = default)
    {
        var existing = await _context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.EventType == eventType, cancellationToken);

        if (existing is not null)
        {
            existing.IsEnabled = isEnabled;
        }
        else
        {
            _context.NotificationPreferences.Add(new NotificationPreference { UserId = userId, EventType = eventType, IsEnabled = isEnabled });
        }
    }
}
