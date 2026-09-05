using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMS.Application.Features.Users.Commands.CreateUser;
using RMS.Application.Features.Users.Commands.SetUserActiveState;
using RMS.Application.Features.Users.Commands.UpdateUser;
using RMS.Application.Features.Users.Commands.UpdateUserRole;
using RMS.Application.Features.Users.DTOs;
using RMS.Application.Features.Users.Queries.GetUserById;
using RMS.Application.Features.Users.Queries.GetUsers;
using RMS.Domain.Enums;

namespace RMS.Api.Controllers;

/// <summary>
/// User directory and admin user/actor management. Reads (GetAll/GetById) stay open to any
/// authenticated user - they're the picker source for approvers/fallback approvers/escalation
/// contacts/delegates across the Feature 3 admin and approver UIs. Every write (Create/Update/
/// activate/deactivate/role change) is System Admin only, matching EligibilityPoliciesController's
/// convention: an [Authorize(Roles=...)] attribute plus a permission-guarded command underneath,
/// belt-and-suspenders. New/edited users need no workflow wiring - ApprovalWorkflowEngine already
/// resolves Role-type approvers by querying Users directly (GetActiveByRoleAsync), so a user becomes
/// assignable the moment their Role/IsActive match, and stops being assignable the moment they don't.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize]
// TODO: real Keycloak login isn't wired on the frontend yet (tracked separately) -
// AllowAnonymous is a temporary local-dev stub so this controller doesn't 401
// every request in the meantime. Remove once login is wired end-to-end.
[AllowAnonymous]
public class UsersController : ControllerBase
{
    private readonly ISender _sender;

    public UsersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserSummaryDto>>> GetAll([FromQuery] UserRole? role, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetUsersQuery(role), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserSummaryDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetUserByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    public record CreateUserBody(
        string FullName, string Email, UserRole Role,
        string? Grade, string? Designation, EmploymentType? EmploymentType, string? Department, string? Location);

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.SystemAdmin))]
    public async Task<ActionResult<UserSummaryDto>> Create(CreateUserBody body, CancellationToken cancellationToken)
    {
        var command = new CreateUserCommand(
            body.FullName, body.Email, body.Role, body.Grade, body.Designation, body.EmploymentType, body.Department, body.Location);
        var result = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    public record UpdateUserBody(
        string FullName, string Email, UserRole Role,
        string? Grade, string? Designation, EmploymentType? EmploymentType, string? Department, string? Location);

    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.SystemAdmin))]
    public async Task<ActionResult<UserSummaryDto>> Update(Guid id, UpdateUserBody body, CancellationToken cancellationToken)
    {
        var command = new UpdateUserCommand(
            id, body.FullName, body.Email, body.Role, body.Grade, body.Designation, body.EmploymentType, body.Department, body.Location);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/activate")]
    [Authorize(Roles = nameof(UserRole.SystemAdmin))]
    public async Task<ActionResult<UserSummaryDto>> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetUserActiveStateCommand(id, true), cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Roles = nameof(UserRole.SystemAdmin))]
    public async Task<ActionResult<UserSummaryDto>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetUserActiveStateCommand(id, false), cancellationToken);
        return Ok(result);
    }

    public record UpdateRoleBody(UserRole NewRole);

    /// <summary>Feature 10 (US-031) - the quick inline role-change action on the user list row.
    /// Permission-guarded (Module=Rbac, Action=Edit) in the handler itself.</summary>
    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleBody body, CancellationToken cancellationToken)
    {
        await _sender.Send(new UpdateUserRoleCommand(id, body.NewRole), cancellationToken);
        return NoContent();
    }
}
