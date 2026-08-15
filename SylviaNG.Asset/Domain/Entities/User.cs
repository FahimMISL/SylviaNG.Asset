using RMS.Domain.Common;
using RMS.Domain.Enums;

namespace RMS.Domain.Entities;

/// <summary>
/// Minimal user/auth foundation per US-046 + US-047 - just enough for a
/// System Admin to log in and be recorded as the actor on Feature 1 actions.
/// Bulk import, HRMS sync, multi-role assignment, SSO and 2FA are out of
/// scope until US-046/US-047 are built in full.
/// </summary>
public class User : AuditableEntity
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
}
