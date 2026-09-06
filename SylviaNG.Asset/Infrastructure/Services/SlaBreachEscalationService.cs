using Microsoft.EntityFrameworkCore;
using RMS.Application.Features.Approvals.Services;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;
using RMS.Infrastructure.Data;

namespace RMS.Infrastructure.Services;

/// <summary>
/// Per the Feature 3 plan: no background-job infrastructure (Hangfire/Quartz) exists anywhere in this
/// codebase, so automatic SLA handling is a plain .NET BackgroundService polling every few minutes.
/// Two jobs per pass: (1) breach escalation - RequisitionApproval rows past SlaDueUtc, EscalateOnBreach
/// true, not yet escalated, routed through the same ApprovalEscalationHelper the manual Escalate action
/// uses, logging SlaBreachEscalation; (2) 50%/80% reminders - written as an AuditLog entry (per the plan)
/// with a dedup check against AuditLogs so each threshold only fires once per approval.
/// </summary>
public class SlaBreachEscalationService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SlaBreachEscalationService> _logger;

    public SlaBreachEscalationService(IServiceProvider serviceProvider, ILogger<SlaBreachEscalationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        do
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SlaBreachEscalationService poll failed.");
            }
        }
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RmsDbContext>();
        var approvalRepository = scope.ServiceProvider.GetRequiredService<IRequisitionApprovalRepository>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTime.UtcNow;

        await RunBreachEscalationAsync(context, approvalRepository, userRepository, notificationService, now, cancellationToken);
        await RunRemindersAsync(context, notificationService, now, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task RunBreachEscalationAsync(
        RmsDbContext context, IRequisitionApprovalRepository approvalRepository, IUserRepository userRepository,
        INotificationService notificationService, DateTime now, CancellationToken cancellationToken)
    {
        var breached = await approvalRepository.GetBreachedUnescalatedAsync(now, cancellationToken);

        foreach (var approval in breached)
        {
            var sla = approval.ApprovalWorkflowStage?.Sla;
            if (sla is null || !sla.EscalateOnBreach)
            {
                continue;
            }

            var targets = await ApprovalEscalationHelper.ResolveEscalationTargetsAsync(
                sla, approval.RequisitionApprovalProcess!.Requisition!.CompanyId, userRepository, cancellationToken);

            if (targets.Count == 0)
            {
                continue;
            }

            ApprovalEscalationHelper.ApplyEscalation(
                approvalRepository, approval, targets, ApprovalActionType.SlaBreachEscalation,
                actorUserId: null, actorName: "System", actorRole: null,
                comment: "Automatically escalated: SLA due date was breached.");

            var requisition = approval.RequisitionApprovalProcess.Requisition!;
            foreach (var targetUserId in targets)
            {
                await notificationService.NotifyAsync(new NotificationRequest(
                    requisition.CompanyId, targetUserId, NotificationEventType.SlaBreachEscalated,
                    requisition.Id,
                    new Dictionary<string, string> { ["RequisitionNumber"] = requisition.RequisitionNumber ?? "N/A" }),
                    cancellationToken);
            }
        }
    }

    private static async Task RunRemindersAsync(
        RmsDbContext context, INotificationService notificationService, DateTime now, CancellationToken cancellationToken)
    {
        var active = await context.RequisitionApprovals
            .Include(a => a.ApprovalWorkflowStage!).ThenInclude(s => s.Sla)
            .Include(a => a.Assignments)
            .Include(a => a.RequisitionApprovalProcess!).ThenInclude(p => p.Requisition)
            .Where(a => (a.Status == RequisitionApprovalStatus.Pending || a.Status == RequisitionApprovalStatus.InProgress)
                && a.SlaStartUtc != null && a.SlaDueUtc != null)
            .ToListAsync(cancellationToken);

        foreach (var approval in active)
        {
            var sla = approval.ApprovalWorkflowStage?.Sla;
            if (sla is null)
            {
                continue;
            }

            var state = SlaStateCalculator.Compute(approval.SlaStartUtc, approval.SlaDueUtc, approval.SlaPausedAtUtc, now);

            if (sla.Reminder50PercentEnabled && state is SlaState.Yellow or SlaState.Red)
            {
                await MaybeSendReminderAsync(context, notificationService, approval, "SlaReminder50", "50%", cancellationToken);
            }

            if (sla.Reminder80PercentEnabled && state == SlaState.Red)
            {
                await MaybeSendReminderAsync(context, notificationService, approval, "SlaReminder80", "80%+", cancellationToken);
            }
        }
    }

    private static async Task MaybeSendReminderAsync(
        RmsDbContext context, INotificationService notificationService, RequisitionApproval approval,
        string actionType, string thresholdLabel, CancellationToken cancellationToken)
    {
        var alreadySent = await context.AuditLogs.AnyAsync(
            l => l.EntityName == nameof(RequisitionApproval) && l.EntityId == approval.Id && l.ActionType == actionType,
            cancellationToken);
        if (alreadySent)
        {
            return;
        }

        context.AuditLogs.Add(new AuditLog
        {
            ActorName = "System",
            ActionType = actionType,
            EntityName = nameof(RequisitionApproval),
            EntityId = approval.Id,
            Details = $"SLA reminder threshold ({thresholdLabel} elapsed) reached.",
            TimestampUtc = DateTime.UtcNow,
        });

        var requisition = approval.RequisitionApprovalProcess?.Requisition;
        if (requisition is null)
        {
            return;
        }

        foreach (var assignment in approval.Assignments.Where(a => !a.HasActed))
        {
            await notificationService.NotifyAsync(new NotificationRequest(
                requisition.CompanyId, assignment.AssignedUserId, NotificationEventType.SlaReminder,
                requisition.Id,
                new Dictionary<string, string> { ["RequisitionNumber"] = requisition.RequisitionNumber ?? "N/A" }),
                cancellationToken);
        }
    }
}
