using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMS.Application.Features.Reporting.DTOs;
using RMS.Application.Features.Reporting.Queries.GetExecutiveSummary;
using RMS.Application.Features.Reporting.Queries.GetOperationalReport;
using RMS.Domain.Enums;

namespace RMS.Api.Controllers;

/// <summary>
/// Feature 7 - Reporting & Analytics (US-022 Operational Requisition Reports, US-025 CEO Executive
/// Summary only - US-023/US-024 are out of scope, see the plan). Role checks live in each query
/// handler (SystemAdmin for the operational report, SystemAdmin/Ceo for the executive summary),
/// same pattern as every other controller in this app - not attribute-based here since that's
/// consistent with how the rest of the codebase enforces access.
/// </summary>
[ApiController]
[Route("api/reporting")]
[Authorize]
// TODO: real Keycloak login isn't wired on the frontend yet (tracked separately) -
// AllowAnonymous is a temporary local-dev stub so this controller doesn't 401
// every request in the meantime. Remove once login is wired end-to-end.
[AllowAnonymous]
public class ReportingController : ControllerBase
{
    private readonly ISender _sender;

    public ReportingController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("operational")]
    public async Task<ActionResult<List<OperationalReportRowDto>>> GetOperationalReport(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? department,
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? categoryItemId,
        [FromQuery] List<RequisitionStatus>? statuses,
        [FromQuery] RequisitionPriority? priority,
        [FromQuery] string? requesterSearch,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetOperationalReportQuery(dateFrom, dateTo, department, categoryId, categoryItemId, statuses, priority, requesterSearch),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("executive-summary")]
    public async Task<ActionResult<ExecutiveSummaryDto>> GetExecutiveSummary(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetExecutiveSummaryQuery(), cancellationToken);
        return Ok(result);
    }
}
