using RMS.Domain.Entities;

namespace RMS.Application.Interfaces;

public interface IRequisitionRepository
{
    Task<Requisition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Requisition>> GetAllForUserAsync(Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HasRequisitionsForCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<bool> AnyFieldValuesExistForCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>FR-RR-004: how many requisitions already have a number for this year, to derive the next sequence.</summary>
    Task<int> CountNumberedInYearAsync(int year, CancellationToken cancellationToken = default);

    /// <summary>FR-RR-011: the most recent match, if any, for the soft duplicate-submission warning.</summary>
    Task<Requisition?> FindPotentialDuplicateAsync(
        Guid userId, Guid categoryId, DateTime needByDate, int totalQuantity, DateTime sinceUtc, CancellationToken cancellationToken = default);

    void Add(Requisition requisition);

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
}
