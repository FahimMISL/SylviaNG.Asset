using RMS.Domain.Entities;

namespace RMS.Application.Interfaces;

public interface IApprovalDelegationRepository
{
    Task<ApprovalDelegation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ApprovalDelegation>> GetForDelegatorAsync(Guid delegatorUserId, CancellationToken cancellationToken = default);
    Task<List<ApprovalDelegation>> GetAllAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>The single active (not revoked, date-in-range) delegation for one delegator on the given
    /// date, if any - the core of the "effective assignee" resolution.</summary>
    Task<ApprovalDelegation?> GetActiveOnAsync(Guid delegatorUserId, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Every delegatorUserId who currently has an active delegation TO delegateUserId on the
    /// given date - i.e. "who am I currently standing in for" - used to widen the pending-approvals query.</summary>
    Task<List<Guid>> GetDelegatorsForAsync(Guid delegateUserId, DateOnly date, CancellationToken cancellationToken = default);

    void Add(ApprovalDelegation delegation);
}
