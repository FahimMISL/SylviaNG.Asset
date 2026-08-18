namespace RMS.Domain.Entities;

/// <summary>
/// US-048: date-ranged out-of-office delegation. Distinct from the ad-hoc single-task "Delegate"
/// action on RequisitionApprovalAssignment - see the Feature 3 plan's "Delegation: two distinct
/// mechanisms" section. This NEVER mutates assignment rows; "effective assignee" is resolved
/// dynamically wherever it matters (pending-approvals query, every action handler's authorization
/// check): effectiveAssignee = ActiveDelegation(assignedUserId, today)?.DelegateUserId ?? assignedUserId.
/// Naturally reversible at the end of the date range with zero batch/revert job.
/// </summary>
public class ApprovalDelegation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid DelegatorUserId { get; set; }
    public User? DelegatorUser { get; set; }

    public Guid DelegateUserId { get; set; }
    public User? DelegateUser { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;

    /// <summary>Differs from DelegatorUserId when a SystemAdmin configures this on someone's behalf.</summary>
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsRevoked { get; set; }

    public bool IsActiveOn(DateOnly date) => !IsRevoked && date >= StartDate && date <= EndDate;
}
