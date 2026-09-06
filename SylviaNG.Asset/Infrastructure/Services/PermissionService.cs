using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Infrastructure.Services;

public class PermissionService : IPermissionService
{
    private readonly IRolePermissionRepository _rolePermissionRepository;

    public PermissionService(IRolePermissionRepository rolePermissionRepository)
    {
        _rolePermissionRepository = rolePermissionRepository;
    }

    public async Task<bool> HasPermissionAsync(
        Guid companyId, UserRole role, PermissionModule module, PermissionAction action, CancellationToken cancellationToken = default)
    {
        if (role == UserRole.SystemAdmin)
        {
            return true;
        }

        var row = await _rolePermissionRepository.GetAsync(companyId, role, module, action, cancellationToken);

        // No row = not yet granted - a missing permission is a denial, not an open default, so a
        // module added later (or a company whose seed hasn't run) fails closed rather than open.
        return row?.IsAllowed ?? false;
    }
}
