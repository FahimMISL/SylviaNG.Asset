using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Interfaces;

public interface IRequisitionRepository
{
    Task<Requisition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Requisition>> GetAllForUserAsync(Guid companyId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Feature 5: every requisition in the procurement pipeline for this company - no
    /// per-user filter, since every Procurement Officer must see every match (no assignment).</summary>
    Task<List<Requisition>> GetForProcurementAsync(Guid companyId, List<RequisitionStatus> statuses, CancellationToken cancellationToken = default);
    Task<bool> HasRequisitionsForCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<bool> AnyFieldValuesExistForCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>FR-RR-004: the highest sequence number already assigned this year, to derive the next
    /// one (highest + 1) - not a count of currently-existing rows, since a deleted requisition (see
    /// DeleteRequisitionCommand's UnderReview case) would otherwise leave a gap that a count-based
    /// "next sequence" collides into.</summary>
    Task<int> GetHighestSequenceInYearAsync(int year, CancellationToken cancellationToken = default);

    /// <summary>FR-RR-011: the most recent match, if any, for the soft duplicate-submission warning.</summary>
    Task<Requisition?> FindPotentialDuplicateAsync(
        Guid userId, Guid categoryId, DateTime needByDate, int totalQuantity, DateTime sinceUtc, CancellationToken cancellationToken = default);

    /// <summary>Feature 4 replacement-rule check: the most recent Approved/PartiallyApproved
    /// transition timestamp (RequisitionStatusHistory.CreatedAtUtc where ToStatus is one of those two)
    /// among this user's requisitions for (CategoryId, CategoryItemId) whose CURRENT status is one of
    /// qualifyingStatuses - i.e. it was actually approved at some point and hasn't since moved to a
    /// status outside that set (Rejected/Cancelled/Draft/SentBack/Submitted/UnderReview requisitions
    /// never count, not even as a "no prior request" fallback). Null if no such requisition exists -
    /// PolicyEvaluationService treats that as "first-time request, allowed".</summary>
    Task<DateTime?> GetMostRecentApprovedTransitionUtcAsync(
        Guid userId, Guid categoryId, Guid? categoryItemId, List<RequisitionStatus> qualifyingStatuses,
        CancellationToken cancellationToken = default);

    void Add(Requisition requisition);

    /// <summary>Permanently removes a Requisition and its owned children (Items, FieldValues,
    /// StatusHistory, Attachments) via the configured cascade-delete relationships. Only ever called on
    /// a Draft - see DeleteRequisitionCommandHandler.</summary>
    void Remove(Requisition requisition);

    /// <summary>
    /// Replaces a requisition's items by adding new ones directly against the
    /// DbSet rather than through the tracked parent's navigation collection -
    /// the same EF Core insert-vs-update tracking fix used for category field
    /// definitions (see CategoryRepository.ReplaceFieldDefinitions).
    /// </summary>
    void ReplaceItems(Requisition requisition, List<RequisitionItem> newItems);
    void ReplaceFieldValues(Requisition requisition, List<RequisitionFieldValue> newValues);
    void AddAttachment(Requisition requisition, RequisitionAttachment attachment);
    void RemoveAttachment(Requisition requisition, RequisitionAttachment attachment);

    /// <summary>Registers a new status-transition entry directly against the DbSet. Required for any
    /// Requisition loaded via GetByIdAsync (already tracked) - see Requisition.Submit's remarks for why
    /// StatusHistory.Add(...) alone isn't reliable in that case. Not needed for a brand-new Requisition
    /// that hasn't been Add()-ed yet, since EF correctly discovers its whole graph as inserts.</summary>
    void AddStatusHistory(RequisitionStatusHistory entry);

    /// <summary>Feature 5: registers a new procurement/fulfillment ledger entry directly against the
    /// DbSet - see AddStatusHistory's remarks for why this bypasses the tracked parent's navigation
    /// collection.</summary>
    void AddProcurementRecord(RequisitionProcurementRecord record);

    /// <summary>Feature 7: every requisition matching the given (all-optional) filters, for the
    /// Operational Report and the Executive Summary (called with every filter null - a full,
    /// unfiltered snapshot). Loads Items+CategoryItem, Category, RequestedByUser, StatusHistory, and
    /// ApprovalProcess+StageInstances+ApprovalWorkflowStage - everything ReportingCalculations needs,
    /// nothing procurement-specific since procurement/approval status here is derived purely from
    /// Requisition.Status, not from RequisitionProcurementRecords.</summary>
    Task<List<Requisition>> GetForReportingAsync(
        Guid companyId, DateTime? dateFrom, DateTime? dateTo, string? department, Guid? categoryId, Guid? categoryItemId,
        List<RequisitionStatus>? statuses, RequisitionPriority? priority, string? requesterSearch,
        CancellationToken cancellationToken = default);
}
