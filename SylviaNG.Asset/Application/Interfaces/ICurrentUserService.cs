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
    bool IsInRole(UserRole role);
}
