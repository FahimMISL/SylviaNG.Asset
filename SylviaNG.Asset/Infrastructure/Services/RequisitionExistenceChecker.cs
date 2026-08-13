using RMS.Application.Interfaces;

namespace RMS.Infrastructure.Services;

/// <summary>
/// Backs the "delete blocked if historical requisitions exist" rule
/// (US-001 edge case) with a real query now that Feature 2's Requisition
/// table exists.
/// </summary>
public class RequisitionExistenceChecker : IRequisitionExistenceChecker
{
    private readonly IRequisitionRepository _requisitionRepository;

    public RequisitionExistenceChecker(IRequisitionRepository requisitionRepository)
    {
        _requisitionRepository = requisitionRepository;
    }

    public Task<bool> HasRequisitionsForCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
        _requisitionRepository.HasRequisitionsForCategoryAsync(categoryId, cancellationToken);
}
