using Microsoft.EntityFrameworkCore;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;
using RMS.Infrastructure.Data;

namespace RMS.Infrastructure.Repositories;

public class RolePermissionRepository : IRolePermissionRepository
{
    private readonly RmsDbContext _context;

    public RolePermissionRepository(RmsDbContext context)
    {
        _context = context;
    }

    public Task<List<RolePermission>> GetForRoleAsync(Guid companyId, UserRole role, CancellationToken cancellationToken = default) =>
        _context.RolePermissions.Where(p => p.CompanyId == companyId && p.Role == role).ToListAsync(cancellationToken);

    public Task<RolePermission?> GetAsync(
        Guid companyId, UserRole role, PermissionModule module, PermissionAction action, CancellationToken cancellationToken = default) =>
        _context.RolePermissions.FirstOrDefaultAsync(
            p => p.CompanyId == companyId && p.Role == role && p.Module == module && p.Action == action, cancellationToken);

    public void Add(RolePermission permission) => _context.RolePermissions.Add(permission);
}
