using RMS.Domain.Entities;

namespace RMS.Application.Interfaces;

public interface IRequisitionRepository
{
    Task<Requisition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Requisition>> GetAllForUserAsync(Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HasRequisitionsForCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<bool> AnyFieldValuesExistForCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
    void Add(Requisition requisition);

    /// <summary>
    /// Replaces a requisition's items by adding new ones directly against the
    /// DbSet rather than through the tracked parent's navigation collection -
    /// the same EF Core insert-vs-update tracking fix used for category field
    /// definitions (see CategoryRepository.ReplaceFieldDefinitions).
    /// </summary>
    void ReplaceItems(Requisition requisition, List<RequisitionItem> newItems);
    void ReplaceFieldValues(Requisition requisition, List<RequisitionFieldValue> newValues);
}
