using Microsoft.EntityFrameworkCore;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Infrastructure.Data;

namespace RMS.Infrastructure.Repositories;

public class ApprovalWorkflowRepository : IApprovalWorkflowRepository
{
    private readonly RmsDbContext _context;

    public ApprovalWorkflowRepository(RmsDbContext context)
    {
        _context = context;
    }

    private IQueryable<ApprovalWorkflow> QueryWithDetails() =>
        _context.ApprovalWorkflows
            .Include(w => w.Versions).ThenInclude(v => v.Stages).ThenInclude(s => s.Approvers)
            .Include(w => w.Versions).ThenInclude(v => v.Stages).ThenInclude(s => s.Conditions)
            .Include(w => w.Versions).ThenInclude(v => v.Stages).ThenInclude(s => s.Sla)
            .Include(w => w.Versions).ThenInclude(v => v.CategoryLinks);

    public Task<ApprovalWorkflow?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        QueryWithDetails().FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public Task<List<ApprovalWorkflow>> GetAllAsync(Guid companyId, bool? isActive, CancellationToken cancellationToken = default)
    {
        var query = QueryWithDetails().Where(w => w.CompanyId == companyId);
        if (isActive.HasValue)
        {
            query = query.Where(w => w.IsActive == isActive.Value);
        }

        return query.OrderBy(w => w.Name).ToListAsync(cancellationToken);
    }

    public Task<bool> NameExistsAsync(Guid companyId, string name, Guid? excludeId, CancellationToken cancellationToken = default) =>
        _context.ApprovalWorkflows.AnyAsync(
            w => w.CompanyId == companyId && w.Name == name && (excludeId == null || w.Id != excludeId),
            cancellationToken);

    public async Task<ApprovalWorkflowVersion?> GetResolvableVersionAsync(Guid companyId, Guid categoryId, CancellationToken cancellationToken = default)
    {
        var candidates = await _context.ApprovalWorkflows
            .Where(w => w.CompanyId == companyId && w.IsActive)
            .Include(w => w.Versions).ThenInclude(v => v.Stages).ThenInclude(s => s.Approvers)
            .Include(w => w.Versions).ThenInclude(v => v.Stages).ThenInclude(s => s.Conditions)
            .Include(w => w.Versions).ThenInclude(v => v.Stages).ThenInclude(s => s.Sla)
            .Include(w => w.Versions).ThenInclude(v => v.CategoryLinks)
            .ToListAsync(cancellationToken);

        foreach (var workflow in candidates)
        {
            var version = workflow.Versions.FirstOrDefault(v => v.VersionNumber == workflow.CurrentVersionNumber && v.IsPublished);
            if (version is null)
            {
                continue;
            }

            if (version.AppliesToAllCategories || version.CategoryLinks.Any(l => l.RequisitionCategoryId == categoryId))
            {
                return version;
            }
        }

        return null;
    }

    public Task<ApprovalWorkflowVersion?> GetVersionByIdAsync(Guid versionId, CancellationToken cancellationToken = default) =>
        _context.ApprovalWorkflowVersions
            .Include(v => v.Stages).ThenInclude(s => s.Approvers)
            .Include(v => v.Stages).ThenInclude(s => s.Conditions)
            .Include(v => v.Stages).ThenInclude(s => s.Sla)
            .Include(v => v.CategoryLinks)
            .FirstOrDefaultAsync(v => v.Id == versionId, cancellationToken);

    public void Add(ApprovalWorkflow workflow) => _context.ApprovalWorkflows.Add(workflow);

    public void AddVersion(ApprovalWorkflowVersion version) => _context.ApprovalWorkflowVersions.Add(version);

    public void ReplaceVersionStages(ApprovalWorkflowVersion version, List<ApprovalWorkflowStage> newStages)
    {
        _context.ApprovalWorkflowStages.RemoveRange(version.Stages);
        version.Stages.Clear();

        // Added directly to the DbSet (whole new subgraph, including each stage's Approvers/
        // Conditions/Sla) so EF Core tracks the entire thing as inserts - see
        // RequisitionRepository.ReplaceItems for the same fix applied here.
        _context.ApprovalWorkflowStages.AddRange(newStages);
    }

    public void ReplaceVersionCategoryLinks(ApprovalWorkflowVersion version, List<Guid> categoryIds)
    {
        _context.ApprovalWorkflowCategoryLinks.RemoveRange(version.CategoryLinks);
        version.CategoryLinks.Clear();

        var newLinks = categoryIds.Select(categoryId => new ApprovalWorkflowCategoryLink
        {
            ApprovalWorkflowVersionId = version.Id,
            RequisitionCategoryId = categoryId,
        }).ToList();

        _context.ApprovalWorkflowCategoryLinks.AddRange(newLinks);
    }

    public void RemoveVersion(ApprovalWorkflowVersion version) => _context.ApprovalWorkflowVersions.Remove(version);
}
