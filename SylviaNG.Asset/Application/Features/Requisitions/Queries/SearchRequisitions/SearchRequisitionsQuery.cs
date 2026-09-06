using MediatR;
using RMS.Application.Common;
using RMS.Application.Features.Requisitions.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Requisitions.Queries.SearchRequisitions;

/// <summary>
/// Feature 11 (Search &amp; Smart Filter). Covers Requisitions, Manpower (a manpower requisition is just
/// a Requisition with Category="Manpower" - the freeText match against Items[].ItemName is what lets
/// searching a Position like "Software Engineer" find it), and Procurement's pipeline slice (via the
/// Statuses filter) - one endpoint, not three, per the task's own "do not create duplicate search
/// systems." Every filter is optional; an all-null query returns everything the caller's role can see,
/// newest first. Data-scope is resolved in the handler and enforced server-side inside the repository
/// query itself, not here and not by the frontend - see SearchRequisitionsQueryHandler's remarks.
/// </summary>
public record SearchRequisitionsQuery(
    string? FreeText,
    string? RequesterSearch,
    List<RequisitionStatus>? Statuses,
    RequisitionPriority? Priority,
    string? Department,
    Guid? CategoryId,
    Guid? CategoryItemId,
    DateTime? DateFrom,
    DateTime? DateTo,
    DateTime? NeedByFrom,
    DateTime? NeedByTo,
    string SortBy = "CreatedAtUtc",
    bool SortDescending = true,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<RequisitionSearchResultDto>>, IPermissionGuardedRequest
{
    public PermissionModule Module => PermissionModule.Search;
    public PermissionAction Action => PermissionAction.View;
}
