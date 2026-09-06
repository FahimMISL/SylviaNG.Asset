using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMS.Application.Features.Dashboard.DTOs;
using RMS.Application.Features.Dashboard.Queries.GetManpowerSummary;

namespace RMS.Api.Controllers;

/// <summary>
/// Feature 12 - Role-Specific Dashboard. Presentation-layer only: every widget's data comes from an
/// existing authorized endpoint (My/Department Requisitions, Procurement Queue, Pending Approvals,
/// Notifications, Executive Summary, Roles) except this one - HR Manager's company-wide manpower
/// summary, the one genuine data gap this feature closes.
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Authorize]
// TODO: real Keycloak login isn't wired on the frontend yet (tracked separately) -
// AllowAnonymous is a temporary local-dev stub so this controller doesn't 401
// every request in the meantime. Remove once login is wired end-to-end.
[AllowAnonymous]
public class DashboardController : ControllerBase
{
    private readonly ISender _sender;

    public DashboardController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("manpower-summary")]
    public async Task<ActionResult<ManpowerSummaryDto>> GetManpowerSummary(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetManpowerSummaryQuery(), cancellationToken);
        return Ok(result);
    }
}
