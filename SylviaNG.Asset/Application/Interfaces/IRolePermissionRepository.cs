using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Interfaces;

public interface IRolePermissionRepository
{
    Task<List<RolePermission>> GetForRoleAsync(Guid companyId, UserRole role, CancellationToken cancellationToken = default);
    Task<RolePermission?> GetAsync(
        Guid companyId, UserRole role, PermissionModule module, PermissionAction action, CancellationToken cancellationToken = default);
    void Add(RolePermission permission);
}
