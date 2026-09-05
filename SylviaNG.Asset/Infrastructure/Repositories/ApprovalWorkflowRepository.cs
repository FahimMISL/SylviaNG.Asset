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

        var currentVersions = candidates
            .Select(w => w.Versions.FirstOrDefault(v => v.VersionNumber == w.CurrentVersionNumber && v.IsPublished))
            .Where(v => v is not null)
            .Select(v => v!)
            .ToList();

        // A workflow explicitly scoped to this category must always win over one that merely applies
        // to every category - otherwise which one resolves is pure luck of query row order (the actual
        // bug: an unrelated "applies to all categories" workflow was silently swallowing every Manpower
        // submission ahead of the real, narrowly-scoped Manpower workflow). Most-specific-wins, checked
        // as its own pass before ever falling back to an all-categories match.
        var specificMatch = currentVersions.FirstOrDefault(v => v.CategoryLinks.Any(l => l.RequisitionCategoryId == categoryId));
        if (specificMatch is not null)
        {
            return specificMatch;
        }

        return currentVersions.FirstOrDefault(v => v.AppliesToAllCategories);
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

    public async Task ReplaceVersionStagesAsync(ApprovalWorkflowVersion version, List<ApprovalWorkflowStage> newStages, CancellationToken cancellationToken = default)
    {
        _context.ApprovalWorkflowStages.RemoveRange(version.Stages);
        version.Stages.Clear();

        // Flush the deletes to the database BEFORE queuing the inserts, in their own SaveChanges.
        // Editing an existing draft (the normal case - "Create New Version" clones the prior version's
        // stages, so there's always something here to replace) reuses the same StageOrder values the
        // old rows already had. IX_ApprovalWorkflowStages_ApprovalWorkflowVersionId_StageOrder is a
        // unique index on (VersionId, StageOrder), and batching the deletes and inserts into one
        // SaveChanges let the new rows' INSERTs reach Postgres while the old rows sharing the same
        // StageOrder were still present, violating that constraint. A brand new workflow's very first
        // save (nothing to remove) never hit this - only ever re-saving an already-populated draft did.
        await _context.SaveChangesAsync(cancellationToken);

        // ApprovalWorkflowStageMapper.ToEntities builds bare stage entities with no way to know their
        // parent version's Id - it's a static mapper with no version context. Set it explicitly here
        // (the one place that actually has `version`), or every new stage inserts with a default/empty
        // ApprovalWorkflowVersionId and violates the FK to ApprovalWorkflowVersions.
        foreach (var stage in newStages)
        {
            stage.ApprovalWorkflowVersionId = version.Id;
        }

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
