using Microsoft.EntityFrameworkCore;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;
using RMS.Infrastructure.Data;

namespace RMS.Infrastructure.Repositories;

public class NotificationTemplateRepository : INotificationTemplateRepository
{
    private readonly RmsDbContext _context;

    public NotificationTemplateRepository(RmsDbContext context)
    {
        _context = context;
    }

    public Task<NotificationTemplate?> GetByEventTypeAsync(Guid companyId, NotificationEventType eventType, CancellationToken cancellationToken = default) =>
        _context.NotificationTemplates.FirstOrDefaultAsync(t => t.CompanyId == companyId && t.EventType == eventType, cancellationToken);

    public Task<List<NotificationTemplate>> GetAllAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        _context.NotificationTemplates.Where(t => t.CompanyId == companyId).ToListAsync(cancellationToken);

    public void Add(NotificationTemplate template) => _context.NotificationTemplates.Add(template);

    public void Remove(NotificationTemplate template) => _context.NotificationTemplates.Remove(template);
}
