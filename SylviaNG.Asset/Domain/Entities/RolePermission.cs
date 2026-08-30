using RMS.Domain.Enums;

namespace RMS.Domain.Entities;

/// <summary>
/// Feature 10 (US-031): one row per (CompanyId, Role, Module, Action). Roles stay the fixed UserRole
/// enum (see the plan's Context - the current architecture stores exactly one role as a single JWT
/// claim, not a dynamic set, so "custom roles" aren't supported without a much larger rewrite) - this
/// table is purely the permission side, not a role-definition table.
/// </summary>
public class RolePermission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public UserRole Role { get; set; }
    public PermissionModule Module { get; set; }
    public PermissionAction Action { get; set; }
    public bool IsAllowed { get; set; }
}
