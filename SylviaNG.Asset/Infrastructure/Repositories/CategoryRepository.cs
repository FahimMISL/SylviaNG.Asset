using Microsoft.EntityFrameworkCore;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Infrastructure.Data;

namespace RMS.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly RmsDbContext _context;

    public CategoryRepository(RmsDbContext context)
    {
        _context = context;
    }

    private IQueryable<RequisitionCategory> QueryWithDetails() =>
        _context.RequisitionCategories
            .Include(c => c.FieldDefinitions).ThenInclude(f => f.Options)
            .Include(c => c.FieldDefinitions).ThenInclude(f => f.ValidationRule)
            .Include(c => c.CostCenterLinks)
            .Include(c => c.Items);

    public Task<RequisitionCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        QueryWithDetails().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<List<RequisitionCategory>> GetAllAsync(Guid companyId, bool? isActive, CancellationToken cancellationToken = default)
    {
        var query = QueryWithDetails().Where(c => c.CompanyId == companyId);
        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        return query.OrderBy(c => c.Name).ToListAsync(cancellationToken);
    }

    public Task<bool> NameExistsAsync(Guid companyId, string name, Guid? excludeId, CancellationToken cancellationToken = default) =>
        _context.RequisitionCategories.AnyAsync(
            c => c.CompanyId == companyId && c.Name == name && (excludeId == null || c.Id != excludeId),
            cancellationToken);

    public void Add(RequisitionCategory category) => _context.RequisitionCategories.Add(category);

    public void Remove(RequisitionCategory category) => _context.RequisitionCategories.Remove(category);

    public void ReplaceFieldDefinitions(RequisitionCategory category, List<CategoryFieldDefinition> newDefinitions)
    {
        _context.CategoryFieldDefinitions.RemoveRange(category.FieldDefinitions);
        category.FieldDefinitions.Clear();

        // Added directly to the DbSet (not via category.FieldDefinitions.AddRange) so
        // EF Core tracks these pre-keyed entities as new inserts rather than
        // mis-detecting them as updates to rows that don't exist yet - the same
        // issue fixed in AddVersion. EF's relationship fixup then populates
        // category.FieldDefinitions automatically; adding manually too would duplicate it.
        _context.CategoryFieldDefinitions.AddRange(newDefinitions);
    }

    public void ReplaceCostCenterLinks(RequisitionCategory category, List<Guid> costCenterIds)
    {
        _context.CategoryCostCenterLinks.RemoveRange(category.CostCenterLinks);
        category.CostCenterLinks.Clear();

        var newLinks = costCenterIds.Select(costCenterId => new CategoryCostCenterLink
        {
            CategoryId = category.Id,
            CostCenterId = costCenterId,
        }).ToList();

        // See ReplaceFieldDefinitions above - EF's relationship fixup populates
        // category.CostCenterLinks automatically once added to its own DbSet.
        _context.CategoryCostCenterLinks.AddRange(newLinks);
    }

    public void AddVersion(CategoryTemplateVersion version) => _context.CategoryTemplateVersions.Add(version);

    public void ReplaceItems(RequisitionCategory category, List<CategoryItem> newItems)
    {
        _context.CategoryItems.RemoveRange(category.Items);
        category.Items.Clear();
        _context.CategoryItems.AddRange(newItems);
    }
}
