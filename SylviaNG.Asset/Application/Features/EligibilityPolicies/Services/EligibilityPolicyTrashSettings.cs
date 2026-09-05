namespace RMS.Application.Features.EligibilityPolicies.Services;

/// <summary>Single source of truth for the Trash retention window, shared by the command handler that
/// computes a policy's PurgeAtUtc for display and by EligibilityPolicyTrashPurgeService's actual sweep
/// - a fixed 30 days, per the feature's own spec ("configurable/fixed retention period").</summary>
public static class EligibilityPolicyTrashSettings
{
    public const int RetentionDays = 30;
}
