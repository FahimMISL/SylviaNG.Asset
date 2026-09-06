using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMS.Api.Controllers.Requests;
using RMS.Application.Features.Approvals.Commands.ApproveApproval;
using RMS.Application.Features.Approvals.Commands.DelegateApprovalAction;
using RMS.Application.Features.Approvals.Commands.EscalateApproval;
using RMS.Application.Features.Approvals.Commands.PartialApproveApproval;
using RMS.Application.Features.Approvals.Commands.RejectApproval;
using RMS.Application.Features.Approvals.Commands.RequestClarification;
using RMS.Application.Features.Approvals.Commands.RespondToClarification;
using RMS.Application.Features.Approvals.Commands.SendBackApproval;
using RMS.Application.Features.Approvals.DTOs;
using RMS.Application.Features.Approvals.Queries.GetPendingApprovals;

namespace RMS.Api.Controllers;

/// <summary>
/// Feature 3 - Approval Workflow Management, approver actions + inbox. Any authenticated approver;
/// each action handler enforces its own delegation-aware authorization (see
/// ApprovalAuthorizationHelper), not role attributes here - who can act on a given approval is
/// data-driven (workflow config), not a static role list.
/// </summary>
[ApiController]
[Route("api/approvals")]
[Authorize]
// TODO: real Keycloak login isn't wired on the frontend yet (tracked separately) -
// AllowAnonymous is a temporary local-dev stub so this controller doesn't 401
// every request in the meantime. Remove once login is wired end-to-end.
[AllowAnonymous]
public class ApprovalsController : ControllerBase
{
    private readonly ISender _sender;

    public ApprovalsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("pending")]
    public async Task<ActionResult<List<PendingApprovalDto>>> GetPending(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPendingApprovalsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, ApproveApprovalRequestBody body, CancellationToken cancellationToken)
    {
        await _sender.Send(new ApproveApprovalCommand(id, body.Comment, body.EstimatedCost), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, ApprovalCommentRequestBody body, CancellationToken cancellationToken)
    {
        await _sender.Send(new RejectApprovalCommand(id, body.Comment), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/send-back")]
    public async Task<IActionResult> SendBack(Guid id, ApprovalCommentRequestBody body, CancellationToken cancellationToken)
    {
        await _sender.Send(new SendBackApprovalCommand(id, body.Comment), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/request-clarification")]
    public async Task<IActionResult> RequestClarification(Guid id, ApprovalCommentRequestBody body, CancellationToken cancellationToken)
    {
        await _sender.Send(new RequestClarificationCommand(id, body.Comment), cancellationToken);
        return NoContent();
    }

    /// <summary>Requestor only - enforced in the handler, not here.</summary>
    [HttpPost("{id:guid}/respond-clarification")]
    public async Task<IActionResult> RespondToClarification(Guid id, ApprovalCommentRequestBody body, CancellationToken cancellationToken)
    {
        await _sender.Send(new RespondToClarificationCommand(id, body.Comment), cancellationToken);
        return NoContent();
    }

    /// <summary>Ad-hoc single-task delegate - not the out-of-office ApprovalDelegation (see
    /// ApprovalDelegationsController for that).</summary>
    [HttpPost("{id:guid}/delegate")]
    public async Task<IActionResult> Delegate(Guid id, DelegateApprovalActionRequestBody body, CancellationToken cancellationToken)
    {
        await _sender.Send(new DelegateApprovalActionCommand(id, body.DelegateToUserId, body.Comment), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/escalate")]
    public async Task<IActionResult> Escalate(Guid id, ApprovalCommentRequestBody body, CancellationToken cancellationToken)
    {
        await _sender.Send(new EscalateApprovalCommand(id, body.Comment), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/partial-approve")]
    public async Task<IActionResult> PartialApprove(Guid id, PartialApproveApprovalRequestBody body, CancellationToken cancellationToken)
    {
        await _sender.Send(new PartialApproveApprovalCommand(id, body.Comment, body.Decisions), cancellationToken);
        return NoContent();
    }
}
