using RMS.Domain.Enums;

namespace RMS.Application.Interfaces;

/// <summary>Resolves the acting user from the current request's JWT claims. Minimal auth foundation per US-047.</summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? FullName { get; }
    UserRole? Role { get; }
    Guid? CompanyId { get; }
    string? IpAddress { get; }

    /// <summary>Feature 10 (US-032): DepartmentHead's own data-scoping key - resolved the same way as
    /// every other claim here (JWT claim when authenticated, dev-stub user lookup otherwise).</summary>
    string? Department { get; }

    bool IsInRole(UserRole role);
}
