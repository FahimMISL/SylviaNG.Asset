namespace RMS.Application.Interfaces;

/// <summary>
/// Stub seam for the "delete blocked if historical requisitions exist" rule
/// (US-001 edge case). Feature 2 doesn't exist yet, so the real
/// implementation always returns false until that feature is built and
/// wires a concrete check against the Requisition table.
/// </summary>
public interface IRequisitionExistenceChecker
{
    Task<bool> HasRequisitionsForCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
}
