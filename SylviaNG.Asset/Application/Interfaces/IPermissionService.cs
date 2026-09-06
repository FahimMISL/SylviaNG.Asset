using RMS.Domain.Enums;

namespace RMS.Application.Interfaces;

/// <summary>Feature 10 (US-031). Backed by RolePermission - SystemAdmin implicitly passes everything,
/// matching how IsInRole(UserRole.SystemAdmin) is already used as a universal bypass throughout the
/// rest of this codebase, so the matrix never has to special-case "can SystemAdmin still get in."</summary>
public interface IPermissionService
{
    Task<bool> HasPermissionAsync(
        Guid companyId, UserRole role, PermissionModule module, PermissionAction action, CancellationToken cancellationToken = default);
}
