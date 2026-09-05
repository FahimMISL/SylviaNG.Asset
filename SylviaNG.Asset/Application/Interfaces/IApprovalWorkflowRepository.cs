using RMS.Domain.Entities;

namespace RMS.Application.Interfaces;

public interface IApprovalWorkflowRepository
{
    Task<ApprovalWorkflow?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ApprovalWorkflow>> GetAllAsync(Guid companyId, bool? isActive, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(Guid companyId, string name, Guid? excludeId, CancellationToken cancellationToken = default);

    /// <summary>The one ApprovalWorkflowVersion an active workflow's CurrentVersionNumber points to,
    /// scoped to (CompanyId, CategoryId) per ApprovalWorkflowEngine.ResolveAndStart. Null if no active,
    /// published workflow applies.</summary>
    Task<ApprovalWorkflowVersion?> GetResolvableVersionAsync(Guid companyId, Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>A single version with its full nested graph (stages/approvers/conditions/SLA/category links).</summary>
    Task<ApprovalWorkflowVersion?> GetVersionByIdAsync(Guid versionId, CancellationToken cancellationToken = default);

    void Add(ApprovalWorkflow workflow);

    /// <summary>Adds a new version directly against the DbSet - required for a version added to an
    /// already-tracked (pre-existing) workflow, see RequisitionRepository.ReplaceItems' remarks for why.</summary>
    void AddVersion(ApprovalWorkflowVersion version);

    /// <summary>Wholesale-replaces a draft (unpublished) version's nested stages (and their
    /// approvers/conditions/SLA), deleting whatever isn't in the new set. Async because it flushes the
    /// deletes with their own SaveChanges before queuing the inserts - see the implementation for why
    /// that intermediate flush is required, not optional.</summary>
    Task ReplaceVersionStagesAsync(ApprovalWorkflowVersion version, List<ApprovalWorkflowStage> newStages, CancellationToken cancellationToken = default);
    void ReplaceVersionCategoryLinks(ApprovalWorkflowVersion version, List<Guid> categoryIds);

    void RemoveVersion(ApprovalWorkflowVersion version);
}
