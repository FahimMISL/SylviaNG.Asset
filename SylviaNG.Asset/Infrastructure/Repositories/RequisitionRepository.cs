using Microsoft.EntityFrameworkCore;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;
using RMS.Infrastructure.Data;

namespace RMS.Infrastructure.Repositories;

public class RequisitionRepository : IRequisitionRepository
{
    private readonly RmsDbContext _context;

    public RequisitionRepository(RmsDbContext context)
    {
        _context = context;
    }

    public Task<Requisition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Requisitions
            .Include(r => r.Items).ThenInclude(i => i.CategoryItem)
            .Include(r => r.Category)
            .Include(r => r.CostCenter)
            .Include(r => r.FieldValues).ThenInclude(v => v.FieldDefinition)
            .Include(r => r.StatusHistory.OrderBy(h => h.CreatedAtUtc))
            .Include(r => r.Attachments.OrderByDescending(a => a.CreatedAtUtc))
            // Feature 3: the resolved approval process, if any (created by ApprovalWorkflowEngine on
            // submit) - needed both by CreateRequisitionCommandHandler's return value and by
            // GetRequisitionByIdQueryHandler's approver-access extension / ApprovalProcessDto mapping.
            .Include(r => r.ApprovalProcess!).ThenInclude(p => p.ApprovalWorkflowVersion!).ThenInclude(v => v.ApprovalWorkflow)
            .Include(r => r.ApprovalProcess!).ThenInclude(p => p.StageInstances).ThenInclude(a => a.ApprovalWorkflowStage!).ThenInclude(s => s.Sla)
            .Include(r => r.ApprovalProcess!).ThenInclude(p => p.StageInstances).ThenInclude(a => a.Assignments).ThenInclude(x => x.AssignedUser)
            .Include(r => r.ApprovalProcess!).ThenInclude(p => p.StageInstances).ThenInclude(a => a.Actions).ThenInclude(act => act.PartialDecisions)
            // Feature 5: procurement/fulfillment ledger, if any (created by ProcurementService) -
            // needed by GetRequisitionByIdQueryHandler's ProcurementDto mapping.
            .Include(r => r.ProcurementRecords.OrderBy(p => p.CreatedAtUtc)).ThenInclude(p => p.LineItems)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<List<Requisition>> GetAllForUserAsync(Guid companyId, Guid userId, CancellationToken cancellationToken = default) =>
        _context.Requisitions
            .Include(r => r.Items)
            .Include(r => r.Category)
            .Where(r => r.CompanyId == companyId && r.RequestedByUserId == userId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    /// <summary>Feature 5: every requisition in the procurement pipeline for this company, regardless
    /// of who requested it - no per-user filter, since rule 1 requires every Procurement Officer to
    /// see every match with no assignment/round-robin.</summary>
    public Task<List<Requisition>> GetForProcurementAsync(Guid companyId, List<RequisitionStatus> statuses, CancellationToken cancellationToken = default) =>
        _context.Requisitions
            .Include(r => r.Items).ThenInclude(i => i.CategoryItem)
            .Include(r => r.Category)
            .Include(r => r.RequestedByUser)
            .Include(r => r.ProcurementRecords)
            // Needed so ProcurementService.GetApprovedCeilings sees any PartialApprovalDecision for a
            // PartiallyApproved requisition's estimated total in the queue list, same as GetByIdAsync.
            .Include(r => r.ApprovalProcess!).ThenInclude(p => p.StageInstances).ThenInclude(a => a.Actions).ThenInclude(act => act.PartialDecisions)
            .Where(r => r.CompanyId == companyId && statuses.Contains(r.Status))
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<bool> HasRequisitionsForCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
        _context.Requisitions.AnyAsync(r => r.CategoryId == categoryId, cancellationToken);

    public Task<bool> AnyFieldValuesExistForCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
        _context.RequisitionFieldValues.AnyAsync(v => v.FieldDefinition!.CategoryId == categoryId, cancellationToken);

    public Task<int> CountNumberedInYearAsync(int year, CancellationToken cancellationToken = default)
    {
        var prefix = $"REQ-{year}-";
        return _context.Requisitions.CountAsync(r => r.RequisitionNumber != null && r.RequisitionNumber.StartsWith(prefix), cancellationToken);
    }

    public Task<Requisition?> FindPotentialDuplicateAsync(
        Guid userId, Guid categoryId, DateTime needByDate, int totalQuantity, DateTime sinceUtc, CancellationToken cancellationToken = default) =>
        _context.Requisitions
            .Include(r => r.Items)
            .Where(r => r.RequestedByUserId == userId
                && r.CategoryId == categoryId
                && r.NeedByDate.HasValue && r.NeedByDate.Value.Date == needByDate.Date
                && r.CreatedAtUtc >= sinceUtc
                // "You may have already SUBMITTED a similar request" only makes sense against
                // something that was actually submitted - matching a Draft (which has no
                // RequisitionNumber yet) showed "(null)" in the warning message.
                && r.Status != Domain.Enums.RequisitionStatus.Draft
                && r.Status != Domain.Enums.RequisitionStatus.Cancelled)
            .OrderByDescending(r => r.CreatedAtUtc)
            .FirstOrDefaultAsync(r => r.Items.Sum(i => i.Quantity) == totalQuantity, cancellationToken);

    public Task<DateTime?> GetMostRecentApprovedTransitionUtcAsync(
        Guid userId, Guid categoryId, Guid? categoryItemId, List<RequisitionStatus> qualifyingStatuses,
        CancellationToken cancellationToken = default) =>
        _context.RequisitionStatusHistories
            .Where(h => (h.ToStatus == RequisitionStatus.Approved || h.ToStatus == RequisitionStatus.PartiallyApproved)
                && h.Requisition!.RequestedByUserId == userId
                && h.Requisition.CategoryId == categoryId
                && h.Requisition.Items.Any(i => i.CategoryItemId == categoryItemId)
                && qualifyingStatuses.Contains(h.Requisition.Status))
            .OrderByDescending(h => h.CreatedAtUtc)
            .Select(h => (DateTime?)h.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public void Add(Requisition requisition) => _context.Requisitions.Add(requisition);

    public void Remove(Requisition requisition) => _context.Requisitions.Remove(requisition);

    public void ReplaceItems(Requisition requisition, List<RequisitionItem> newItems)
    {
        _context.RequisitionItems.RemoveRange(requisition.Items);
        requisition.Items.Clear();

        foreach (var item in newItems)
        {
            item.RequisitionId = requisition.Id;
        }

        // Added directly to the DbSet (not via requisition.Items.AddRange) so EF Core
        // tracks these as new inserts instead of mis-detecting them as updates -
        // see CategoryRepository.ReplaceFieldDefinitions for the same fix.
        _context.RequisitionItems.AddRange(newItems);
    }

    public void AddStatusHistory(RequisitionStatusHistory entry) => _context.RequisitionStatusHistories.Add(entry);

    /// <summary>Feature 5: registers a new procurement/fulfillment ledger entry directly against the
    /// DbSet - same tracking-bug workaround as AddStatusHistory, needed since the parent Requisition
    /// is already tracked by the time a command handler calls this.</summary>
    public void AddProcurementRecord(RequisitionProcurementRecord record) => _context.RequisitionProcurementRecords.Add(record);

    public void AddAttachment(Requisition requisition, RequisitionAttachment attachment)
    {
        attachment.RequisitionId = requisition.Id;
        requisition.Attachments.Add(attachment);
        _context.RequisitionAttachments.Add(attachment);
    }

    public void RemoveAttachment(Requisition requisition, RequisitionAttachment attachment)
    {
        requisition.Attachments.Remove(attachment);
        _context.RequisitionAttachments.Remove(attachment);
    }

    public void ReplaceFieldValues(Requisition requisition, List<RequisitionFieldValue> newValues)
    {
        _context.RequisitionFieldValues.RemoveRange(requisition.FieldValues);
        requisition.FieldValues.Clear();

        foreach (var value in newValues)
        {
            value.RequisitionId = requisition.Id;
        }

        _context.RequisitionFieldValues.AddRange(newValues);
    }
}
