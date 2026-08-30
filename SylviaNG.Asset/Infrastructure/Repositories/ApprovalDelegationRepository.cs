using Microsoft.EntityFrameworkCore;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Infrastructure.Data;

namespace RMS.Infrastructure.Repositories;

public class ApprovalDelegationRepository : IApprovalDelegationRepository
{
    private readonly RmsDbContext _context;

    public ApprovalDelegationRepository(RmsDbContext context)
    {
        _context = context;
    }

    private IQueryable<ApprovalDelegation> QueryWithDetails() =>
        _context.ApprovalDelegations.Include(d => d.DelegatorUser).Include(d => d.DelegateUser);

    public Task<ApprovalDelegation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        QueryWithDetails().FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<List<ApprovalDelegation>> GetForDelegatorAsync(Guid delegatorUserId, CancellationToken cancellationToken = default) =>
        QueryWithDetails()
            .Where(d => d.DelegatorUserId == delegatorUserId)
            .OrderByDescending(d => d.StartDate)
            .ToListAsync(cancellationToken);

    public Task<List<ApprovalDelegation>> GetAllAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        QueryWithDetails()
            .Where(d => d.CompanyId == companyId)
            .OrderByDescending(d => d.StartDate)
            .ToListAsync(cancellationToken);

    public Task<ApprovalDelegation?> GetActiveOnAsync(Guid delegatorUserId, DateOnly date, CancellationToken cancellationToken = default) =>
        _context.ApprovalDelegations
            .Where(d => d.DelegatorUserId == delegatorUserId && !d.IsRevoked && d.StartDate <= date && d.EndDate >= date)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<List<Guid>> GetDelegatorsForAsync(Guid delegateUserId, DateOnly date, CancellationToken cancellationToken = default) =>
        _context.ApprovalDelegations
            .Where(d => d.DelegateUserId == delegateUserId && !d.IsRevoked && d.StartDate <= date && d.EndDate >= date)
            .Select(d => d.DelegatorUserId)
            .ToListAsync(cancellationToken);

    public void Add(ApprovalDelegation delegation) => _context.ApprovalDelegations.Add(delegation);
}
