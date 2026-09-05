using RMS.Application.Features.EligibilityPolicies.Services;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Infrastructure.Data;

namespace RMS.Infrastructure.Services;

/// <summary>
/// Feature 4 Trash: same plain-BackgroundService pattern as SlaBreachEscalationService (no Hangfire/
/// Quartz anywhere in this codebase). Every pass, permanently removes any soft-deleted
/// EligibilityPolicy whose retention window (EligibilityPolicyTrashSettings.RetentionDays, fixed at
/// 30 days) has elapsed - a real hard delete, exactly what PermanentlyDeleteEligibilityPolicyCommand
/// does on request, just system-triggered instead of admin-triggered. Runs across every company in one
/// pass since this is a maintenance job, not a per-tenant read.
///
/// Audit entries are written directly against AuditLog here (like SlaBreachEscalationService's own
/// reminder path) rather than through IAuditLogger - that service resolves CompanyId from the
/// ambient ICurrentUserService, which has no HttpContext to read inside a background job; the purged
/// policy's own CompanyId is used instead.
/// </summary>
public class EligibilityPolicyTrashPurgeService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(1);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EligibilityPolicyTrashPurgeService> _logger;

    public EligibilityPolicyTrashPurgeService(IServiceProvider serviceProvider, ILogger<EligibilityPolicyTrashPurgeService> logger)
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
                _logger.LogError(ex, "EligibilityPolicyTrashPurgeService poll failed.");
            }
        }
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RmsDbContext>();
        var policyRepository = scope.ServiceProvider.GetRequiredService<IEligibilityPolicyRepository>();

        var cutoffUtc = DateTime.UtcNow.AddDays(-EligibilityPolicyTrashSettings.RetentionDays);
        var expired = await policyRepository.GetExpiredTrashAsync(cutoffUtc, cancellationToken);

        if (expired.Count == 0)
        {
            return;
        }

        foreach (var policy in expired)
        {
            context.AuditLogs.Add(new AuditLog
            {
                CompanyId = policy.CompanyId,
                ActorName = "System",
                ActionType = "EligibilityPolicyPermanentlyDeleted",
                EntityName = nameof(EligibilityPolicy),
                EntityId = policy.Id,
                Details = $"Name={policy.Name}; Reason=Retention period ({EligibilityPolicyTrashSettings.RetentionDays} days) elapsed.",
                TimestampUtc = DateTime.UtcNow,
            });

            policyRepository.Remove(policy);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
