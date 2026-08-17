using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMS.Api.Controllers.Requests;
using RMS.Application.Features.CostCenters.Commands.CreateCostCenter;
using RMS.Application.Features.CostCenters.Commands.DeleteCostCenter;
using RMS.Application.Features.CostCenters.Commands.SetCostCenterActiveState;
using RMS.Application.Features.CostCenters.Commands.UpdateCostCenter;
using RMS.Application.Features.CostCenters.DTOs;
using RMS.Application.Features.CostCenters.Queries.GetCostCenters;
using RMS.Domain.Enums;

namespace RMS.Api.Controllers;

/// <summary>
/// Cost center master data backing US-003 (Feature 1). Reading (GetAll) is open
/// to any authenticated user, since Feature 2 employees need to see a category's
/// linked cost centers to submit a requisition. Every write action stays System
/// Admin only. Responses are wrapped by ResponseWrappingMiddleware - handlers
/// return the raw DTO here, matching AssetController's convention.
/// </summary>
[ApiController]
[Route("api/cost-centers")]
[Authorize]
// TODO: real Keycloak login isn't wired on the frontend yet (tracked separately) -
// AllowAnonymous is a temporary local-dev stub so this controller doesn't 401
// every request in the meantime. Remove once login is wired end-to-end.
[AllowAnonymous]
public class CostCentersController : ControllerBase
{
    private readonly ISender _sender;

    public CostCentersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<List<CostCenterDto>>> GetAll([FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCostCentersQuery(isActive), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.SystemAdmin))]
    public async Task<ActionResult<CostCenterDto>> Create(CreateCostCenterCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.SystemAdmin))]
    public async Task<ActionResult<CostCenterDto>> Update(Guid id, UpdateCostCenterRequestBody body, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpdateCostCenterCommand(id, body.Code, body.Name), cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/activate")]
    [Authorize(Roles = nameof(UserRole.SystemAdmin))]
    public async Task<ActionResult<CostCenterDto>> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetCostCenterActiveStateCommand(id, true), cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Roles = nameof(UserRole.SystemAdmin))]
    public async Task<ActionResult<CostCenterDto>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetCostCenterActiveStateCommand(id, false), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.SystemAdmin))]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteCostCenterCommand(id), cancellationToken);
        return NoContent();
    }
}
