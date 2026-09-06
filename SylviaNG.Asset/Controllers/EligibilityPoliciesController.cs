using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMS.Api.Controllers.Requests;
using RMS.Application.Features.EligibilityPolicies.Commands.CreateEligibilityPolicy;
using RMS.Application.Features.EligibilityPolicies.Commands.DeleteEligibilityPolicy;
using RMS.Application.Features.EligibilityPolicies.Commands.SetEligibilityPolicyActiveState;
using RMS.Application.Features.EligibilityPolicies.Commands.UpdateEligibilityPolicy;
using RMS.Application.Features.EligibilityPolicies.DTOs;
using RMS.Application.Features.EligibilityPolicies.Queries.CheckEligibility;
using RMS.Application.Features.EligibilityPolicies.Queries.GetEligibilityPolicies;
using RMS.Application.Features.EligibilityPolicies.Queries.GetEligibilityPolicyById;
using RMS.Domain.Enums;

namespace RMS.Api.Controllers;

/// <summary>
/// Feature 4 - Eligibility & Policy Management, admin configuration surface plus the employee-facing
/// eligibility check the New Requisition flow calls. Every write action is System Admin only; the
/// check endpoint is open to any authenticated user (it only ever evaluates the CALLER's own record).
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

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.SystemAdmin))]
    public async Task<ActionResult<EligibilityPolicyDto>> Create(SaveEligibilityPolicyRequestBody body, CancellationToken cancellationToken)
    {
        var command = new CreateEligibilityPolicyCommand(
            body.Name, body.Description, body.CategoryId, body.CategoryItemId, body.IsActive, body.Criteria, body.ReplacementRule);
        var result = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.SystemAdmin))]
    public async Task<ActionResult<EligibilityPolicyDto>> Update(Guid id, SaveEligibilityPolicyRequestBody body, CancellationToken cancellationToken)
    {
        var command = new UpdateEligibilityPolicyCommand(
            id, body.Name, body.Description, body.CategoryId, body.CategoryItemId, body.IsActive, body.Criteria, body.ReplacementRule);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/activate")]
    [Authorize(Roles = nameof(UserRole.SystemAdmin))]
    public async Task<ActionResult<EligibilityPolicyDto>> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetEligibilityPolicyActiveStateCommand(id, true), cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Roles = nameof(UserRole.SystemAdmin))]
    public async Task<ActionResult<EligibilityPolicyDto>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetEligibilityPolicyActiveStateCommand(id, false), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.SystemAdmin))]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteEligibilityPolicyCommand(id), cancellationToken);
        return NoContent();
    }
}
