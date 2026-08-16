using Microsoft.EntityFrameworkCore;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
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
            .Include(r => r.Items)
            .Include(r => r.Category)
            .Include(r => r.CostCenter)
            .Include(r => r.FieldValues).ThenInclude(v => v.FieldDefinition)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<List<Requisition>> GetAllForUserAsync(Guid companyId, Guid userId, CancellationToken cancellationToken = default) =>
        _context.Requisitions
            .Include(r => r.Items)
            .Include(r => r.Category)
            .Where(r => r.CompanyId == companyId && r.RequestedByUserId == userId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<bool> HasRequisitionsForCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
        _context.Requisitions.AnyAsync(r => r.CategoryId == categoryId, cancellationToken);

    public Task<bool> AnyFieldValuesExistForCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
        _context.RequisitionFieldValues.AnyAsync(v => v.FieldDefinition!.CategoryId == categoryId, cancellationToken);

    public void Add(Requisition requisition) => _context.Requisitions.Add(requisition);

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
