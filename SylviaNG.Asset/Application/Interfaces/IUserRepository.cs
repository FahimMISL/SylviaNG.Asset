using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Interfaces;

/// <summary>
/// Minimal read-only user lookup, needed by ApprovalWorkflowEngine to resolve Role-type approvers
/// ("Role -> matching Users in company") - not itself part of the Feature 3 plan's explicit
/// repository list, but nothing pre-existing covers this query.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<User>> GetActiveByRoleAsync(Guid companyId, UserRole role, CancellationToken cancellationToken = default);
    Task<List<User>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>Backs GET /api/users - the directory/picker source for approvers/fallback
    /// approvers/escalation contacts/delegates in the admin and approver UIs. Company-scoped,
    /// optionally filtered by role; includes inactive users (the frontend needs to show/label them,
    /// not just active ones like the engine's own GetActiveByRoleAsync).</summary>
    Task<List<User>> GetAllAsync(Guid companyId, UserRole? role, CancellationToken cancellationToken = default);
}
