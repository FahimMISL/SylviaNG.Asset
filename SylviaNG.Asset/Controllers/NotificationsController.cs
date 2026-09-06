using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMS.Application.Features.Notifications.Commands.MarkAllAsRead;
using RMS.Application.Features.Notifications.Commands.MarkAsRead;
using RMS.Application.Features.Notifications.Commands.UpdatePreferences;
using RMS.Application.Features.Notifications.DTOs;
using RMS.Application.Features.Notifications.Queries.GetMyNotifications;
using RMS.Application.Features.Notifications.Queries.GetMyPreferences;
using RMS.Application.Features.Notifications.Queries.GetUnreadCount;

namespace RMS.Api.Controllers;

/// <summary>
/// Feature 9 - Notification Center (US-028/US-029/US-030-delivery). Every endpoint here is scoped to
/// the current user's own notifications/preferences - no role check needed, everyone has exactly one
/// inbox. See NotificationTemplatesController for the SystemAdmin-only template management side.
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
// TODO: real Keycloak login isn't wired on the frontend yet (tracked separately) -
// AllowAnonymous is a temporary local-dev stub so this controller doesn't 401
// every request in the meantime. Remove once login is wired end-to-end.
[AllowAnonymous]
public class NotificationsController : ControllerBase
{
    private readonly ISender _sender;

    public NotificationsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<NotificationPageDto>> GetMyNotifications(
        [FromQuery] bool unreadOnly, [FromQuery] int skip, [FromQuery] int take, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMyNotificationsQuery(unreadOnly, skip, take <= 0 ? 20 : take), cancellationToken);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetUnreadCountQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new MarkAsReadCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        await _sender.Send(new MarkAllAsReadCommand(), cancellationToken);
        return NoContent();
    }

    [HttpGet("preferences")]
    public async Task<ActionResult<List<NotificationPreferenceDto>>> GetPreferences(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMyPreferencesQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesCommand command, CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
        return NoContent();
    }
}
