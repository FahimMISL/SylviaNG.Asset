using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMS.Api.Controllers.Requests;
using RMS.Application.Features.Requisitions.Commands.CancelRequisition;
using RMS.Application.Features.Requisitions.Commands.CreateRequisition;
using RMS.Application.Features.Requisitions.Commands.DeleteRequisition;
using RMS.Application.Features.Requisitions.Commands.DeleteRequisitionAttachment;
using RMS.Application.Features.Requisitions.Commands.UpdateRequisition;
using RMS.Application.Features.Requisitions.Commands.UploadRequisitionAttachment;
using RMS.Application.Features.Requisitions.DTOs;
using RMS.Application.Features.Requisitions.Queries.CheckDuplicateRequisition;
using RMS.Application.Features.Requisitions.Queries.GetMyRequisitions;
using RMS.Application.Features.Requisitions.Queries.GetRequisitionAttachmentDownload;
using RMS.Application.Features.Requisitions.Queries.GetRequisitionById;

namespace RMS.Api.Controllers;

/// <summary>
/// Feature 2 - Requisition Request Management (US-004 through US-007). Any authenticated user.
/// Responses are wrapped into the app's standard {hasError, decentMessage, content}
/// envelope by ResponseWrappingMiddleware - handlers return the raw DTO here,
/// matching AssetController's convention.
/// </summary>
[ApiController]
[Route("api/requisitions")]
[Authorize]
// TODO: real Keycloak login isn't wired on the frontend yet (tracked separately) -
// AllowAnonymous is a temporary local-dev stub so this controller doesn't 401
// every request in the meantime. Remove once login is wired end-to-end.
[AllowAnonymous]
public class RequisitionsController : ControllerBase
{
    private readonly ISender _sender;

    public RequisitionsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>The current user's own requisitions - backs "My Requisitions" / "Continue Draft".</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<List<RequisitionSummaryDto>>> GetMine(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMyRequisitionsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RequisitionDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetRequisitionByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>FR-RR-011: soft duplicate-submission warning, checked before Create/Update actually submits.</summary>
    [HttpGet("duplicate-check")]
    public async Task<ActionResult<DuplicateCheckResultDto>> CheckDuplicate(
        [FromQuery] Guid categoryId, [FromQuery] DateTime? needByDate, [FromQuery] int totalQuantity, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CheckDuplicateRequisitionQuery(categoryId, needByDate, totalQuantity), cancellationToken);
        return Ok(result);
    }

    /// <summary>Creates a new requisition, either as a Draft (Save Draft) or Submitted (Submit Requisition).</summary>
    [HttpPost]
    public async Task<ActionResult<RequisitionDto>> Create(SaveRequisitionRequestBody body, CancellationToken cancellationToken)
    {
        var command = new CreateRequisitionCommand(
            body.CategoryId, body.Items, body.Priority, body.NeedByDate, body.EstimatedCost, body.Justification,
            body.UrgencyJustification, body.CostCenterId, body.ProjectCode, body.FieldValues, body.Submit);
        var result = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Edits an existing Draft ("Continue Draft") or a SentBack requisition (amendment), either
    /// saving/re-saving it or submitting/resubmitting it.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RequisitionDto>> Update(Guid id, SaveRequisitionRequestBody body, CancellationToken cancellationToken)
    {
        var command = new UpdateRequisitionCommand(
            id, body.CategoryId, body.Items, body.Priority, body.NeedByDate, body.EstimatedCost, body.Justification,
            body.UrgencyJustification, body.CostCenterId, body.ProjectCode, body.FieldValues, body.ResubmitComment, body.Submit);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>FR-RR-008: cancel a Submitted requisition before any approver has acted.</summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<RequisitionDto>> Cancel(Guid id, CancelRequisitionRequestBody body, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CancelRequisitionCommand(id, body.Comment), cancellationToken);
        return Ok(result);
    }

    /// <summary>US-007: upload a supporting document. Multipart form, one file per request.</summary>
    [HttpPost("{id:guid}/attachments")]
    [RequestSizeLimit(100_000_000)]
    public async Task<ActionResult<RequisitionAttachmentDto>> UploadAttachment(
        Guid id, IFormFile file, [FromForm] string? reason, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var command = new UploadRequisitionAttachmentCommand(id, file.FileName, file.ContentType, file.Length, stream, reason);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>US-007 AC7: direct link to download a specific attachment.</summary>
    [HttpGet("{id:guid}/attachments/{attachmentId:guid}/download")]
    public async Task<IActionResult> DownloadAttachment(Guid id, Guid attachmentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetRequisitionAttachmentDownloadQuery(id, attachmentId), cancellationToken);
        return File(result.Content, result.ContentType, result.FileName);
    }

    [HttpDelete("{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DeleteAttachment(Guid id, Guid attachmentId, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteRequisitionAttachmentCommand(id, attachmentId), cancellationToken);
        return NoContent();
    }

    /// <summary>Permanently deletes a Draft requisition - not yet in the approval workflow, so there's
    /// nothing to cancel/undo, just remove it. Submitted (or later) requisitions must use Cancel instead.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteRequisitionCommand(id), cancellationToken);
        return NoContent();
    }
}
