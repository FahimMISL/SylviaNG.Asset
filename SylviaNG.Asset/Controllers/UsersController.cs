using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMS.Application.Features.Users.Commands.UpdateUserRole;
using RMS.Application.Features.Users.DTOs;
using RMS.Application.Features.Users.Queries.GetUsers;
using RMS.Domain.Enums;

namespace RMS.Api.Controllers;

/// <summary>
/// Minimal user directory - the picker source for approvers/fallback approvers/escalation contacts/
/// delegates across the Feature 3 admin and approver UIs, so those don't have to fall back to raw
/// GUID text inputs. Read-only, any authenticated user (it's just a picker, not an admin surface).
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

    public record UpdateRoleBody(UserRole NewRole);

    /// <summary>Feature 10 (US-031) - the one write capability this feature adds to this previously
    /// read-only directory. Permission-guarded (Module=Rbac, Action=Edit) in the handler itself.</summary>
    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleBody body, CancellationToken cancellationToken)
    {
        await _sender.Send(new UpdateUserRoleCommand(id, body.NewRole), cancellationToken);
        return NoContent();
    }
}
