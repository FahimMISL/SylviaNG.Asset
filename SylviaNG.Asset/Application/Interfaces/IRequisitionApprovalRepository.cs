using RMS.Domain.Entities;

namespace RMS.Application.Interfaces;

public interface IRequisitionApprovalRepository
{
    /// <summary>Full graph: StageInstances -> ApprovalWorkflowStage(+Sla), Assignments, Actions(+PartialDecisions).</summary>
    Task<RequisitionApprovalProcess?> GetProcessByRequisitionIdAsync(Guid requisitionId, CancellationToken cancellationToken = default);

    Task<RequisitionApprovalProcess?> GetProcessByIdAsync(Guid processId, CancellationToken cancellationToken = default);

    /// <summary>One stage-instance, fully loaded (Process->Requisition, Stage->Approvers/Conditions/Sla,
    /// Assignments, Actions) - what every approval-action handler starts from.</summary>
    Task<RequisitionApproval?> GetApprovalByIdAsync(Guid approvalId, CancellationToken cancellationToken = default);

    /// <summary>Not-yet-acted assignments, on a Pending/InProgress stage, for any of the given (already
    /// delegation-resolved) candidate AssignedUserIds. Backs GetPendingApprovalsQuery.</summary>
    Task<List<RequisitionApprovalAssignment>> GetPendingAssignmentsAsync(List<Guid> candidateAssignedUserIds, CancellationToken cancellationToken = default);

    /// <summary>Every user ever assigned anywhere in this process - used by the delegation-aware
    /// read-access check GetRequisitionByIdQueryHandler was extended with.</summary>
    Task<List<Guid>> GetDistinctAssignedUserIdsAsync(Guid requisitionApprovalProcessId, CancellationToken cancellationToken = default);

    /// <summary>SLA-breached, not-yet-escalated approvals still Pending/InProgress - backs SlaBreachEscalationService.</summary>
    Task<List<RequisitionApproval>> GetBreachedUnescalatedAsync(DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>Pending/InProgress approvals whose SLA reminder threshold hasn't been logged yet - backs
    /// SlaBreachEscalationService's 50%/80% reminder pass.</summary>
    Task<List<RequisitionApproval>> GetActiveWithSlaAsync(CancellationToken cancellationToken = default);

    void AddProcess(RequisitionApprovalProcess process);
    void AddApproval(RequisitionApproval approval);
    void AddAssignment(RequisitionApprovalAssignment assignment);
    void AddAction(RequisitionApprovalAction action);
}
