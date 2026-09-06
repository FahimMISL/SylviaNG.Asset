using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMS.Api.Controllers.Requests;
using RMS.Application.Features.Procurement.Commands.CloseProcurement;
using RMS.Application.Features.Procurement.Commands.RecordFulfillment;
using RMS.Application.Features.Procurement.Commands.StartProcurement;
using RMS.Application.Features.Procurement.DTOs;
using RMS.Application.Features.Procurement.Queries.GetProcurementRequisitions;

namespace RMS.Api.Controllers;

/// <summary>
/// Feature 5 - Procurement & Fulfillment. Any Procurement Officer or SystemAdmin; every requisition
/// in the pipeline is visible/actionable to all of them (rule 1 - no per-officer assignment), unlike
/// Approval's dynamic per-assignment authorization.
/// </summary>
[ApiController]
[Route("api/procurement")]
[Authorize]
// TODO: real Keycloak login isn't wired on the frontend yet (tracked separately) -
// AllowAnonymous is a temporary local-dev stub so this controller doesn't 401
// every request in the meantime. Remove once login is wired end-to-end.
[AllowAnonymous]
public class ProcurementController : ControllerBase
{
    private readonly ISender _sender;

    public ProcurementController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("requisitions")]
    public async Task<ActionResult<List<ProcurementQueueItemDto>>> GetRequisitions(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetProcurementRequisitionsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> Start(Guid id, ProcurementCommentRequestBody body, CancellationToken cancellationToken)
    {
        await _sender.Send(new StartProcurementCommand(id, body.Comment), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/fulfill")]
    public async Task<IActionResult> Fulfill(Guid id, RecordFulfillmentRequestBody body, CancellationToken cancellationToken)
    {
        await _sender.Send(new RecordFulfillmentCommand(id, body.Items, body.Comment), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, ProcurementCommentRequestBody body, CancellationToken cancellationToken)
    {
        await _sender.Send(new CloseProcurementCommand(id, body.Comment), cancellationToken);
        return NoContent();
    }
}
