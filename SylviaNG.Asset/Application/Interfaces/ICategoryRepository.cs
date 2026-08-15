using RMS.Domain.Entities;

namespace RMS.Application.Interfaces;

public interface ICategoryRepository
{
    Task<RequisitionCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<RequisitionCategory>> GetAllAsync(Guid companyId, bool? isActive, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(Guid companyId, string name, Guid? excludeId, CancellationToken cancellationToken = default);
    void Add(RequisitionCategory category);
    void Remove(RequisitionCategory category);

    /// <summary>Replaces all field definitions (and their options/validation rules) on a tracked category, deleting the ones no longer present.</summary>
    void ReplaceFieldDefinitions(RequisitionCategory category, List<CategoryFieldDefinition> newDefinitions);

    /// <summary>Replaces the set of linked cost centers on a tracked category.</summary>
    void ReplaceCostCenterLinks(RequisitionCategory category, List<Guid> costCenterIds);

    /// <summary>Adds a new immutable template version snapshot for a category (US-001 AC6 - Publish).</summary>
    void AddVersion(CategoryTemplateVersion version);
    void ReplaceItems(RequisitionCategory category, List<CategoryItem> newItems);
}
