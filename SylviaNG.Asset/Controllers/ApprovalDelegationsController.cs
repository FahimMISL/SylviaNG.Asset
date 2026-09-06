using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMS.Api.Controllers.Requests;
using RMS.Application.Features.Approvals.Commands.CreateDelegation;
using RMS.Application.Features.Approvals.Commands.RevokeDelegation;
using RMS.Application.Features.Approvals.DTOs;
using RMS.Application.Features.Approvals.Queries.GetDelegations;
using RMS.Application.Features.Approvals.Queries.GetMyDelegations;
using RMS.Domain.Enums;

namespace RMS.Api.Controllers;

/// <summary>Feature 3 - US-048 out-of-office delegation (distinct from the ad-hoc single-task
/// Delegate action on ApprovalsController).</summary>
[ApiController]
[Route("api/approval-delegations")]
[Authorize]
// TODO: real Keycloak login isn't wired on the frontend yet (tracked separately) -
// AllowAnonymous is a temporary local-dev stub so this controller doesn't 401
// every request in the meantime. Remove once login is wired end-to-end.
[AllowAnonymous]
public class ApprovalDelegationsController : ControllerBase
{
    private readonly ISender _sender;

    public ApprovalDelegationsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<ActionResult<ApprovalDelegationDto>> Create(CreateDelegationRequestBody body, CancellationToken cancellationToken)
    {
        var command = new CreateDelegationCommand(body.DelegateUserId, body.StartDate, body.EndDate, body.Reason, body.OnBehalfOfUserId);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet("mine")]
    public async Task<ActionResult<List<ApprovalDelegationDto>>> GetMine(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMyDelegationsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = nameof(UserRole.SystemAdmin))]
    public async Task<ActionResult<List<ApprovalDelegationDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDelegationsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Revoke(Guid id, RevokeDelegationRequestBody body, CancellationToken cancellationToken)
    {
        await _sender.Send(new RevokeDelegationCommand(id, body.Reason), cancellationToken);
        return NoContent();
    }
}
