namespace RMS.Domain.Entities;

/// <summary>
/// Child of RequisitionApproval - the resolved, per-approver row. Also carries the Npgsql xmin
/// concurrency token (configured in EF config) so a double-action on the same assignment surfaces
/// as a conflict. Pending-approvals query = assignments where effective-assignee (delegation-aware,
/// resolved dynamically, never stored here) = me AND HasActed=false AND owning approval.Status in
/// (Pending, InProgress). Parallel-stage completion = every required "slot" satisfied - see
/// RoleFanoutGroupId below for what a slot means when a Role-type approver resolves to several users.
/// </summary>
public class RequisitionApprovalAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RequisitionApprovalId { get; set; }
    public RequisitionApproval? RequisitionApproval { get; set; }

    public Guid AssignedUserId { get; set; }
    public User? AssignedUser { get; set; }

    /// <summary>Set only for ad-hoc single-task delegation (the "Delegate" action) - provenance of who
    /// this assignment was originally meant for before AssignedUserId was overwritten. NOT used for
    /// out-of-office ApprovalDelegation, which never mutates this row at all.</summary>
    public Guid? OriginalApproverUserId { get; set; }

    /// <summary>Null for a SpecificUser-sourced assignment (or a Role that resolved to exactly one
    /// user) - it stands alone as its own required slot. Set to a shared value for every assignment
    /// fanned out from the SAME Role-type WorkflowApprover row when that role has multiple active
    /// users in the company - "any one of them" satisfies that row's requirement (first responder
    /// wins), not all of them. All of them still get the assignment (so all see it in their inbox),
    /// but once one acts the others in the same group are closed out - see
    /// ApprovalWorkflowEngine.CloseRoleFanoutSiblings.</summary>
    public Guid? RoleFanoutGroupId { get; set; }

    public bool IsRequired { get; set; } = true;
    public bool HasActed { get; set; }
    public DateTime? ActedAtUtc { get; set; }
}
