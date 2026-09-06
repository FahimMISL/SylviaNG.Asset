using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMS.Application.Features.AuditLogs.DTOs;
using RMS.Application.Features.AuditLogs.Queries.GetAuditLog;
using RMS.Application.Features.AuditLogs.Services;

namespace RMS.Api.Controllers;

/// <summary>
/// Feature 8 - Audit & Compliance (US-026 Full Immutable Audit Trail only - US-027 compliance
/// enforcement is explicitly out of scope, see the plan). SystemAdmin-only, enforced in the query
/// handler - same pattern as ReportingController.
/// </summary>
[ApiController]
[Route("api/audit-log")]
[Authorize]
// TODO: real Keycloak login isn't wired on the frontend yet (tracked separately) -
// AllowAnonymous is a temporary local-dev stub so this controller doesn't 401
// every request in the meantime. Remove once login is wired end-to-end.
[AllowAnonymous]
public class AuditLogController : ControllerBase
{
    private readonly ISender _sender;

    public AuditLogController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] Guid? requisitionId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? actorSearch,
        [FromQuery] string? actionType,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? department,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetAuditLogQuery(requisitionId, dateFrom, dateTo, actorSearch, actionType, categoryId, department, page, pageSize),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string format,
        [FromQuery] Guid? requisitionId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? actorSearch,
        [FromQuery] string? actionType,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? department,
        CancellationToken cancellationToken)
    {
        // Reuses the exact same authorized/filtered query as Get() above (Feature 11: PageSize set to
        // "everything" here specifically, since the whole point of exporting is every matching row, not
        // one page) - the exported file's rows are guaranteed identical to whatever's on screen for the
        // same filters.
        var result = await _sender.Send(
            new GetAuditLogQuery(requisitionId, dateFrom, dateTo, actorSearch, actionType, categoryId, department, Page: 1, PageSize: int.MaxValue),
            cancellationToken);
        var entries = result.Items;

        if (string.IsNullOrWhiteSpace(format))
        {
            return BadRequest("format is required and must be 'csv' or 'xlsx'.");
        }

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        return format.ToLowerInvariant() switch
        {
            "csv" => File(AuditLogExportService.ToCsv(entries), "text/csv", $"audit-log-{timestamp}.csv"),
            "xlsx" => File(
                AuditLogExportService.ToXlsx(entries),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"audit-log-{timestamp}.xlsx"),
            _ => BadRequest("format must be 'csv' or 'xlsx'."),
        };
    }
}
