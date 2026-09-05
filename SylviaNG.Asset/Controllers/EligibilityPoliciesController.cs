using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMS.Api.Controllers.Requests;
using RMS.Application.Features.EligibilityPolicies.Commands.CreateEligibilityPolicy;
using RMS.Application.Features.EligibilityPolicies.Commands.DeleteEligibilityPolicy;
using RMS.Application.Features.EligibilityPolicies.Commands.PermanentlyDeleteEligibilityPolicy;
using RMS.Application.Features.EligibilityPolicies.Commands.RestoreEligibilityPolicy;
using RMS.Application.Features.EligibilityPolicies.Commands.SetEligibilityPolicyActiveState;
using RMS.Application.Features.EligibilityPolicies.Commands.UpdateEligibilityPolicy;
using RMS.Application.Features.EligibilityPolicies.DTOs;
using RMS.Application.Features.EligibilityPolicies.Queries.CheckEligibility;
using RMS.Application.Features.EligibilityPolicies.Queries.GetEligibilityPolicies;
using RMS.Application.Features.EligibilityPolicies.Queries.GetEligibilityPolicyById;
using RMS.Application.Features.EligibilityPolicies.Queries.GetMyEligibilityPolicyPermissions;
using RMS.Application.Features.EligibilityPolicies.Queries.GetTrashedEligibilityPolicies;
using RMS.Domain.Enums;

namespace RMS.Api.Controllers;

/// <summary>
/// Feature 4 - Eligibility &amp; Policy Management, admin configuration surface plus the employee-facing
/// eligibility check the New Requisition flow calls. Write actions are gated by the live Roles &amp;
/// Permissions matrix (Module=EligibilityPolicy) in each command/query itself - NOT hardcoded to
/// SystemAdmin any more (see GetMyEligibilityPolicyPermissionsQuery, which the frontend uses to show/
/// hide actions to match). The one exception is Permanent Delete, which by design stays SystemAdmin-
/// only regardless of what the matrix grants for the ordinary Delete action - see
/// PermanentlyDeleteEligibilityPolicyCommandHandler.
/// </summary>
[ApiController]
[Route("api/eligibility-policies")]
[Authorize]
// TODO: real Keycloak login isn't wired on the frontend yet (tracked separately) -
// AllowAnonymous is a temporary local-dev stub so this controller doesn't 401
// every request in the meantime. Remove once login is wired end-to-end.
[AllowAnonymous]
public class EligibilityPoliciesController : ControllerBase
{
    private readonly ISender _sender;

    public EligibilityPoliciesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<List<EligibilityPolicySummaryDto>>> GetAll([FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetEligibilityPoliciesQuery(isActive), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EligibilityPolicyDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetEligibilityPolicyByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>Called from the New Requisition flow after the employee picks a Category and Type/
    /// Item - always evaluates the current authenticated user, never a client-supplied one.</summary>
    [HttpGet("check")]
    public async Task<ActionResult<EligibilityCheckResultDto>> Check(
        [FromQuery] Guid categoryId, [FromQuery] Guid? categoryItemId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CheckEligibilityQuery(categoryId, categoryItemId), cancellationToken);
        return Ok(result);
    }

    /// <summary>Drives the Eligibility &amp; Policies screen's action buttons - see the class remarks.</summary>
    [HttpGet("my-permissions")]
    public async Task<ActionResult<EligibilityPolicyMyPermissionsDto>> GetMyPermissions(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMyEligibilityPolicyPermissionsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<EligibilityPolicyDto>> Create(SaveEligibilityPolicyRequestBody body, CancellationToken cancellationToken)
    {
        var command = new CreateEligibilityPolicyCommand(
            body.Name, body.Description, body.CategoryId, body.CategoryItemId, body.IsActive, body.Criteria, body.ReplacementRule);
        var result = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EligibilityPolicyDto>> Update(Guid id, SaveEligibilityPolicyRequestBody body, CancellationToken cancellationToken)
    {
        var command = new UpdateEligibilityPolicyCommand(
            id, body.Name, body.Description, body.CategoryId, body.CategoryItemId, body.IsActive, body.Criteria, body.ReplacementRule);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/activate")]
    public async Task<ActionResult<EligibilityPolicyDto>> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetEligibilityPolicyActiveStateCommand(id, true), cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<ActionResult<EligibilityPolicyDto>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetEligibilityPolicyActiveStateCommand(id, false), cancellationToken);
        return Ok(result);
    }

    /// <summary>Moves the policy to Trash - does NOT permanently delete it, see PermanentDelete below.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteEligibilityPolicyCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpGet("trash")]
    public async Task<ActionResult<List<EligibilityPolicyTrashDto>>> GetTrash(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetTrashedEligibilityPoliciesQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<ActionResult<EligibilityPolicyDto>> Restore(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RestoreEligibilityPolicyCommand(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>Irreversible - see PermanentlyDeleteEligibilityPolicyCommandHandler. SystemAdmin-only
    /// by design, enforced in the handler; the attribute here just documents that intent (this
    /// controller's class-level [AllowAnonymous] means it isn't the actual enforcement).</summary>
    [HttpDelete("{id:guid}/permanent")]
    [Authorize(Roles = nameof(UserRole.SystemAdmin))]
    public async Task<IActionResult> PermanentDelete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new PermanentlyDeleteEligibilityPolicyCommand(id), cancellationToken);
        return NoContent();
    }
}
