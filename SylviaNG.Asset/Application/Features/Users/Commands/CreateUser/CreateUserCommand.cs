using MediatR;
using RMS.Application.Features.Users.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Users.Commands.CreateUser;

/// <summary>Admin user/actor management: creates a new user directly from the UI (previously only
/// possible via the dev seeder). New users are Active by default and immediately become eligible for
/// Role-type approver resolution (ApprovalWorkflowEngine.ResolveApproversAsync ->
/// IUserRepository.GetActiveByRoleAsync) as soon as their Role matches a workflow stage - no workflow
/// configuration or per-actor wiring required. Permission-guarded (Module=Rbac, Action=Create),
/// matching the existing UpdateUserRoleCommand's use of the Rbac module for user-directory writes.</summary>
public record CreateUserCommand(
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
    public PermissionAction Action => PermissionAction.Create;
}
