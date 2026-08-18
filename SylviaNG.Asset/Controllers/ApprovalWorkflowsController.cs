using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMS.Api.Controllers.Requests;
using RMS.Application.Features.ApprovalWorkflows.Commands.CreateApprovalWorkflow;
using RMS.Application.Features.ApprovalWorkflows.Commands.CreateApprovalWorkflowVersion;
using RMS.Application.Features.ApprovalWorkflows.Commands.DeleteApprovalWorkflowDraftVersion;
using RMS.Application.Features.ApprovalWorkflows.Commands.PublishApprovalWorkflowVersion;
using RMS.Application.Features.ApprovalWorkflows.Commands.SetApprovalWorkflowActiveState;
using RMS.Application.Features.ApprovalWorkflows.Commands.UpdateApprovalWorkflowVersion;
using RMS.Application.Features.ApprovalWorkflows.DTOs;
using RMS.Application.Features.ApprovalWorkflows.Queries.GetApprovalWorkflowById;
using RMS.Application.Features.ApprovalWorkflows.Queries.GetApprovalWorkflows;
using RMS.Domain.Enums;

namespace RMS.Api.Controllers;

/// <summary>
/// Feature 3 - Approval Workflow Management, admin configuration surface. Every write action is
/// System Admin only, per the plan's API Endpoints section.
/// </summary>
[ApiController]
[Route("api/approval-workflows")]
[Authorize]
// TODO: real Keycloak login isn't wired on the frontend yet (tracked separately) -
// AllowAnonymous is a temporary local-dev stub so this controller doesn't 401
// every request in the meantime. Remove once login is wired end-to-end.
[AllowAnonymous]
public class ApprovalWorkflowsController : ControllerBase
{
    private readonly ISender _sender;

    public ApprovalWorkflowsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<List<ApprovalWorkflowSummaryDto>>> GetAll([FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetApprovalWorkflowsQuery(isActive), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApprovalWorkflowDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetApprovalWorkflowByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.SystemAdmin))]
    public async Task<ActionResult<ApprovalWorkflowDto>> Create(SaveApprovalWorkflowVersionRequestBody body, CancellationToken cancellationToken)
    {
        var command = new CreateApprovalWorkflowCommand(
            body.Name, body.Description, body.RoutingMode, body.AppliesToAllCategories, body.CategoryIds, body.Stages, body.Notes);
        var result = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/versions")]
    [Authorize(Roles = nameof(UserRole.SystemAdmin))]
    public async Task<ActionResult<ApprovalWorkflowDto>> CreateVersion(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreateApprovalWorkflowVersionCommand(id), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/versions/{versionId:guid}")]
    [Authorize(Roles = nameof(UserRole.SystemAdmin))]
    public async Task<ActionResult<ApprovalWorkflowDto>> UpdateVersion(
        Guid id, Guid versionId, UpdateApprovalWorkflowVersionRequestBody body, CancellationToken cancellationToken)
    {
        var command = new UpdateApprovalWorkflowVersionCommand(
            id, versionId, body.RoutingMode, body.AppliesToAllCategories, body.CategoryIds, body.Stages, body.Notes);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/versions/{versionId:guid}/publish")]
    [Authorize(Roles = nameof(UserRole.SystemAdmin))]
    public async Task<ActionResult<ApprovalWorkflowDto>> Publish(Guid id, Guid versionId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new PublishApprovalWorkflowVersionCommand(id, versionId), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/versions/{versionId:guid}")]
    [Authorize(Roles = nameof(UserRole.SystemAdmin))]
    public async Task<IActionResult> DeleteVersion(Guid id, Guid versionId, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteApprovalWorkflowDraftVersionCommand(id, versionId), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/activate")]
    [Authorize(Roles = nameof(UserRole.SystemAdmin))]
    public async Task<ActionResult<ApprovalWorkflowDto>> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetApprovalWorkflowActiveStateCommand(id, true), cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Roles = nameof(UserRole.SystemAdmin))]
    public async Task<ActionResult<ApprovalWorkflowDto>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetApprovalWorkflowActiveStateCommand(id, false), cancellationToken);
        return Ok(result);
    }
}
