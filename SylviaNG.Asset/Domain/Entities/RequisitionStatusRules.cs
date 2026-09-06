using RMS.Domain.Enums;

namespace RMS.Domain.Entities;

/// <summary>
/// FR-RR-006: the single source of truth for which status transitions are
/// legal. Requisition's own transition methods (Submit/Cancel/SendBack/
/// Resubmit) consult this instead of setting Status directly, so no caller -
/// UI or otherwise - can force an arbitrary status change. Transitions owned
/// by Feature 3 (Approval Workflow) are listed here too so this stays the one
/// place that defines the whole lifecycle, even though Feature 2 never
/// triggers them itself.
/// </summary>
public static class RequisitionStatusRules
{
    private static readonly Dictionary<RequisitionStatus, RequisitionStatus[]> Transitions = new()
    {
        [RequisitionStatus.Draft] = [RequisitionStatus.Submitted],
        [RequisitionStatus.Submitted] = [RequisitionStatus.UnderReview, RequisitionStatus.Cancelled],
        [RequisitionStatus.UnderReview] = [RequisitionStatus.Approved, RequisitionStatus.Rejected, RequisitionStatus.PartiallyApproved, RequisitionStatus.SentBack],
        [RequisitionStatus.SentBack] = [RequisitionStatus.Submitted],
        [RequisitionStatus.Approved] = [RequisitionStatus.InProcurement],
        [RequisitionStatus.PartiallyApproved] = [RequisitionStatus.InProcurement],
        [RequisitionStatus.InProcurement] = [RequisitionStatus.Fulfilled, RequisitionStatus.PartiallyFulfilled],
        [RequisitionStatus.Fulfilled] = [RequisitionStatus.Closed],
        [RequisitionStatus.PartiallyFulfilled] = [RequisitionStatus.Fulfilled, RequisitionStatus.Closed],
        [RequisitionStatus.Rejected] = [],
        [RequisitionStatus.Closed] = [],
        [RequisitionStatus.Cancelled] = [],
    };

    public static bool CanTransition(RequisitionStatus from, RequisitionStatus to) =>
        Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
}
