using MediatR;
using RMS.Application.Features.Users.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Users.Commands.UpdateUser;

/// <summary>Admin user/actor management: full edit of an existing user's basic info, role and
/// eligibility attributes in one call (IsActive is deliberately excluded - that's
/// SetUserActiveStateCommand's job, same split as every other admin entity in this codebase, e.g.
/// EligibilityPolicy's Update vs SetEligibilityPolicyActiveState). Permission-guarded (Module=Rbac,
/// Action=Edit), same module UpdateUserRoleCommand already uses for user-directory writes.</summary>
public record UpdateUserCommand(
    Guid UserId,
    string FullName,
    string Email,
    UserRole Role,
    string? Grade,
    string? Designation,
    EmploymentType? EmploymentType,
    string? Department,
    string? Location) : IRequest<UserSummaryDto>, IPermissionGuardedRequest
{
    public PermissionModule Module => PermissionModule.Rbac;
    public PermissionAction Action => PermissionAction.Edit;
}
