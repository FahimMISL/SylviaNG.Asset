using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMS.Api.Controllers.Requests;
using RMS.Application.Features.RequisitionCategories.Commands.CloneCategory;
using RMS.Application.Features.RequisitionCategories.Commands.CreateCategory;
using RMS.Application.Features.RequisitionCategories.Commands.DeleteCategory;
using RMS.Application.Features.RequisitionCategories.Commands.PublishCategory;
using RMS.Application.Features.RequisitionCategories.Commands.SetCategoryActiveState;
using RMS.Application.Features.RequisitionCategories.Commands.UpdateCategory;
using RMS.Application.Features.RequisitionCategories.DTOs;
using RMS.Application.Features.RequisitionCategories.Queries.GetCategories;
using RMS.Application.Features.RequisitionCategories.Queries.GetCategoryById;
using RMS.Domain.Enums;

namespace RMS.Api.Controllers;

/// <summary>
/// Feature 1 - Requisition Setup & Configuration (US-001, US-002, US-003).
/// Reading (GetAll/GetById/Preview) is open to any authenticated user, since
/// Feature 2 employees need to browse active categories to start a
/// requisition (US-004 AC2). Every write action stays System Admin only.
/// Responses are wrapped by ResponseWrappingMiddleware - handlers return the
/// raw DTO here, matching AssetController's convention.
/// </summary>
[ApiController]
[Route("api/requisition-categories")]
[Authorize]
// TODO: real Keycloak login isn't wired on the frontend yet (tracked separately) -
// AllowAnonymous is a temporary local-dev stub so this controller doesn't 401
// every request in the meantime. Remove once login is wired end-to-end.
[AllowAnonymous]
public class RequisitionCategoriesController : ControllerBase
{
    private readonly ISender _sender;

    public RequisitionCategoriesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<List<CategorySummaryDto>>> GetAll([FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCategoriesQuery(isActive), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCategoryByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>US-001 AC5: Employee View preview - same read model, framed for the builder's preview pane.</summary>
    [HttpGet("{id:guid}/preview")]
    public async Task<ActionResult<CategoryDto>> Preview(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCategoryByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.SystemAdmin))]
    public async Task<ActionResult<CategoryDto>> Create(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.SystemAdmin))]
    public async Task<ActionResult<CategoryDto>> Update(Guid id, UpdateCategoryRequestBody body, CancellationToken cancellationToken)
    {
        var command = new UpdateCategoryCommand(
            id, body.Name, body.Description, body.IsCostCenterMandatory,
            body.ProjectCodeRequirement, body.CostCenterIds, body.FieldDefinitions, body.Items);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/activate")]
    [Authorize(Roles = nameof(UserRole.SystemAdmin))]
    public async Task<ActionResult<CategoryDto>> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetCategoryActiveStateCommand(id, true), cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Roles = nameof(UserRole.SystemAdmin))]
    public async Task<ActionResult<CategoryDto>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetCategoryActiveStateCommand(id, false), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = nameof(UserRole.SystemAdmin))]
    public async Task<ActionResult<CategoryDto>> Publish(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new PublishCategoryCommand(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/clone")]
    [Authorize(Roles = nameof(UserRole.SystemAdmin))]
    public async Task<ActionResult<CategoryDto>> Clone(Guid id, CloneCategoryRequestBody body, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CloneCategoryCommand(id, body.NewName), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.SystemAdmin))]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteCategoryCommand(id), cancellationToken);
        return NoContent();
    }
}
