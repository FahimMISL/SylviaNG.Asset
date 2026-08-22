using Microsoft.Extensions.Configuration;
using RMS.Application.Features.Notifications.Services;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;
using RMS.Infrastructure.Data;

namespace RMS.Infrastructure.Services;

/// <summary>
/// Feature 9: replaces NoOpNotificationService. Checks the recipient's preference (unless the event
/// is critical - see NotificationEventTypeCatalog), resolves a template (customized row, else
/// DefaultNotificationTemplates), renders it, persists the in-app Notification, and logs delivery via
/// the existing IAuditLogger (Feature 8) rather than a second audit table.
///
/// Saves eagerly (own SaveChangesAsync), same reasoning as AuditLogger: this is called from many
/// different command handlers, sometimes after their own SaveChangesAsync already ran, sometimes from
/// a non-HTTP background-service scope (SlaBreachEscalationService) - it can't rely on a caller's unit
/// of work to persist what it writes.
///
/// Email is designed for (EmailSubject/EmailBody are rendered) but not actually sent this pass - no
/// SMTP transport exists in this project yet. That's recorded as a "NotificationSkipped" delivery-log
/// entry with a clear reason, not silently dropped.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly RmsDbContext _context;
    private readonly INotificationPreferenceRepository _preferenceRepository;
    private readonly INotificationTemplateRepository _templateRepository;
    private readonly IAuditLogger _auditLogger;
    private readonly string _frontendBaseUrl;

    public NotificationService(
        RmsDbContext context, INotificationPreferenceRepository preferenceRepository,
        INotificationTemplateRepository templateRepository, IAuditLogger auditLogger, IConfiguration configuration)
    {
        _context = context;
        _preferenceRepository = preferenceRepository;
        _templateRepository = templateRepository;
        _auditLogger = auditLogger;
        _frontendBaseUrl = configuration["Frontend:BaseUrl"] ?? "http://localhost:4200";
    }

    public async Task NotifyAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        var isCritical = NotificationEventTypeCatalog.IsCritical(request.EventType);

        if (!isCritical)
        {
            var enabled = await _preferenceRepository.IsEnabledAsync(request.RecipientUserId, request.EventType, cancellationToken);
            if (!enabled)
            {
                return;
            }
        }

        var customTemplate = await _templateRepository.GetByEventTypeAsync(request.CompanyId, request.EventType, cancellationToken);
        var defaultContent = DefaultNotificationTemplates.For(request.EventType);

        // Computed once here rather than by every call site, so "Link" is always available and
        // always points at the same place - a handler only needs to supply RequisitionId.
        var mergeTags = request.RequisitionId is { } requisitionId && !request.MergeTags.ContainsKey("Link")
            ? new Dictionary<string, string>(request.MergeTags) { ["Link"] = $"{_frontendBaseUrl}/requisitions/{requisitionId}" }
            : request.MergeTags;

        var emailSubject = MergeTagRenderer.Render(customTemplate?.EmailSubject ?? defaultContent.EmailSubject, mergeTags);
        var inAppMessage = MergeTagRenderer.Render(customTemplate?.InAppMessage ?? defaultContent.InAppMessage, mergeTags);

        var notification = new Notification
        {
            CompanyId = request.CompanyId,
            RecipientUserId = request.RecipientUserId,
            EventType = request.EventType,
            RequisitionId = request.RequisitionId,
            Subject = emailSubject,
            Message = inAppMessage,
            IsCritical = isCritical,
        };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            "NotificationDelivered", nameof(Notification), notification.Id,
            $"Channel=InApp; Recipient={request.RecipientUserId}; EventType={request.EventType}; Status=Delivered", cancellationToken);
        await _auditLogger.LogAsync(
            "NotificationSkipped", nameof(Notification), notification.Id,
            $"Channel=Email; Recipient={request.RecipientUserId}; EventType={request.EventType}; Status=Skipped; Reason=No SMTP transport configured in this environment", cancellationToken);
    }
}
