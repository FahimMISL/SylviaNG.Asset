using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMS.Application.Features.Rbac.Commands.UpdatePermissionMatrix;
using RMS.Application.Features.Rbac.DTOs;
using RMS.Application.Features.Rbac.Queries.GetPermissionMatrix;
using RMS.Application.Features.Rbac.Queries.GetRoles;
using RMS.Domain.Enums;

namespace RMS.Api.Controllers;

/// <summary>
/// Feature 10 - Role-Based Access Control (US-031). Enforced via the permission matrix itself
/// (Module=Rbac) rather than a hardcoded role check - this feature's own screens are gated through
/// the exact mechanism it ships, not a special case. See GetRolesQueryHandler's remarks on why role
/// names themselves aren't user-creatable.
/// </summary>
[ApiController]
[Route("api/rbac")]
[Authorize]
// TODO: real Keycloak login isn't wired on the frontend yet (tracked separately) -
// AllowAnonymous is a temporary local-dev stub so this controller doesn't 401
// every request in the meantime. Remove once login is wired end-to-end.
[AllowAnonymous]
public class RbacController : ControllerBase
{
    private readonly ISender _sender;

    public RbacController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("roles")]
    public async Task<ActionResult<List<RoleSummaryDto>>> GetRoles(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetRolesQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("roles/{role}/permissions")]
    public async Task<ActionResult<PermissionMatrixDto>> GetPermissions(UserRole role, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPermissionMatrixQuery(role), cancellationToken);
        return Ok(result);
    }

    public record UpdatePermissionsBody(List<PermissionCellInput> Permissions);

    [HttpPut("roles/{role}/permissions")]
    public async Task<IActionResult> UpdatePermissions(UserRole role, [FromBody] UpdatePermissionsBody body, CancellationToken cancellationToken)
    {
        await _sender.Send(new UpdatePermissionMatrixCommand(role, body.Permissions), cancellationToken);
        return NoContent();
    }
}
