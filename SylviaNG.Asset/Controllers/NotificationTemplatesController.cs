using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMS.Application.Features.NotificationTemplates.Commands.ResetTemplate;
using RMS.Application.Features.NotificationTemplates.Commands.UpdateTemplate;
using RMS.Application.Features.NotificationTemplates.DTOs;
using RMS.Application.Features.NotificationTemplates.Queries.GetTemplates;
using RMS.Domain.Enums;

namespace RMS.Api.Controllers;

/// <summary>Feature 9 (US-058) - SystemAdmin-only, enforced in each handler (same pattern as
/// AuditLogController/ReportingController).</summary>
[ApiController]
[Route("api/notification-templates")]
[Authorize]
// TODO: real Keycloak login isn't wired on the frontend yet (tracked separately) -
// AllowAnonymous is a temporary local-dev stub so this controller doesn't 401
// every request in the meantime. Remove once login is wired end-to-end.
[AllowAnonymous]
public class NotificationTemplatesController : ControllerBase
{
    private readonly ISender _sender;

    public NotificationTemplatesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<List<NotificationTemplateDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetTemplatesQuery(), cancellationToken);
        return Ok(result);
    }

    public record UpdateTemplateBody(string EmailSubject, string EmailBody, string InAppMessage);

    [HttpPut("{eventType}")]
    public async Task<IActionResult> Update(NotificationEventType eventType, [FromBody] UpdateTemplateBody body, CancellationToken cancellationToken)
    {
        await _sender.Send(new UpdateTemplateCommand(eventType, body.EmailSubject, body.EmailBody, body.InAppMessage), cancellationToken);
        return NoContent();
    }

    [HttpPost("{eventType}/reset")]
    public async Task<IActionResult> Reset(NotificationEventType eventType, CancellationToken cancellationToken)
    {
        await _sender.Send(new ResetTemplateCommand(eventType), cancellationToken);
        return NoContent();
    }
}
