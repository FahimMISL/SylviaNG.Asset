using Microsoft.EntityFrameworkCore;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;
using RMS.Infrastructure.Data;

namespace RMS.Infrastructure.Repositories;

public class RequisitionApprovalRepository : IRequisitionApprovalRepository
{
    private readonly RmsDbContext _context;

    public RequisitionApprovalRepository(RmsDbContext context)
    {
        _context = context;
    }

    private IQueryable<RequisitionApprovalProcess> ProcessQueryWithDetails() =>
        _context.RequisitionApprovalProcesses
            .Include(p => p.Requisition)
            .Include(p => p.ApprovalWorkflowVersion!).ThenInclude(v => v.ApprovalWorkflow)
            .Include(p => p.StageInstances).ThenInclude(a => a.ApprovalWorkflowStage!).ThenInclude(s => s.Sla)
            .Include(p => p.StageInstances).ThenInclude(a => a.Assignments)
            .Include(p => p.StageInstances).ThenInclude(a => a.Actions).ThenInclude(act => act.PartialDecisions);

    public Task<RequisitionApprovalProcess?> GetProcessByRequisitionIdAsync(Guid requisitionId, CancellationToken cancellationToken = default) =>
        ProcessQueryWithDetails().FirstOrDefaultAsync(p => p.RequisitionId == requisitionId, cancellationToken);

    public Task<RequisitionApprovalProcess?> GetProcessByIdAsync(Guid processId, CancellationToken cancellationToken = default) =>
        ProcessQueryWithDetails().FirstOrDefaultAsync(p => p.Id == processId, cancellationToken);

    public Task<RequisitionApproval?> GetApprovalByIdAsync(Guid approvalId, CancellationToken cancellationToken = default) =>
        _context.RequisitionApprovals
            .Include(a => a.RequisitionApprovalProcess!).ThenInclude(p => p.Requisition!).ThenInclude(r => r.Items)
            .Include(a => a.ApprovalWorkflowStage!).ThenInclude(s => s.Approvers)
            .Include(a => a.ApprovalWorkflowStage!).ThenInclude(s => s.Conditions)
            .Include(a => a.ApprovalWorkflowStage!).ThenInclude(s => s.Sla)
            .Include(a => a.Assignments)
            .Include(a => a.Actions)
            .FirstOrDefaultAsync(a => a.Id == approvalId, cancellationToken);

    public Task<List<RequisitionApprovalAssignment>> GetPendingAssignmentsAsync(List<Guid> candidateAssignedUserIds, CancellationToken cancellationToken = default) =>
        _context.RequisitionApprovalAssignments
            .Include(x => x.RequisitionApproval!).ThenInclude(a => a.ApprovalWorkflowStage!)
            .Include(x => x.RequisitionApproval!).ThenInclude(a => a.RequisitionApprovalProcess!).ThenInclude(p => p.Requisition!).ThenInclude(r => r.Category)
            .Where(x => candidateAssignedUserIds.Contains(x.AssignedUserId)
                && !x.HasActed
                && (x.RequisitionApproval!.Status == RequisitionApprovalStatus.Pending
                    || x.RequisitionApproval.Status == RequisitionApprovalStatus.InProgress))
            .ToListAsync(cancellationToken);

    public async Task<List<Guid>> GetDistinctAssignedUserIdsAsync(Guid requisitionApprovalProcessId, CancellationToken cancellationToken = default) =>
        await _context.RequisitionApprovalAssignments
            .Where(x => x.RequisitionApproval!.RequisitionApprovalProcessId == requisitionApprovalProcessId)
            .Select(x => x.AssignedUserId)
            .Distinct()
            .ToListAsync(cancellationToken);

    public Task<List<RequisitionApproval>> GetBreachedUnescalatedAsync(DateTime nowUtc, CancellationToken cancellationToken = default) =>
        _context.RequisitionApprovals
            .Include(a => a.ApprovalWorkflowStage!).ThenInclude(s => s.Sla)
            .Include(a => a.Actions)
            .Include(a => a.RequisitionApprovalProcess!).ThenInclude(p => p.Requisition)
            .Where(a => (a.Status == RequisitionApprovalStatus.Pending || a.Status == RequisitionApprovalStatus.InProgress)
                && a.SlaDueUtc != null && a.SlaDueUtc < nowUtc
                && !a.Actions.Any(act => act.ActionType == ApprovalActionType.SlaBreachEscalation))
            .ToListAsync(cancellationToken);

    public Task<List<RequisitionApproval>> GetActiveWithSlaAsync(CancellationToken cancellationToken = default) =>
        _context.RequisitionApprovals
            .Include(a => a.ApprovalWorkflowStage!).ThenInclude(s => s.Sla)
            .Include(a => a.Actions)
            .Where(a => (a.Status == RequisitionApprovalStatus.Pending || a.Status == RequisitionApprovalStatus.InProgress)
                && a.SlaDueUtc != null)
            .ToListAsync(cancellationToken);

    public void AddProcess(RequisitionApprovalProcess process) => _context.RequisitionApprovalProcesses.Add(process);

    public void AddApproval(RequisitionApproval approval) => _context.RequisitionApprovals.Add(approval);

    public void AddAssignment(RequisitionApprovalAssignment assignment) => _context.RequisitionApprovalAssignments.Add(assignment);

    public void AddAction(RequisitionApprovalAction action) => _context.RequisitionApprovalActions.Add(action);
}
