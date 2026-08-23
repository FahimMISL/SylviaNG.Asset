using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Common;
using RMS.Application.Features.Requisitions.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Requisitions.Queries.SearchRequisitions;

public class SearchRequisitionsQueryHandler : IRequestHandler<SearchRequisitionsQuery, PagedResult<RequisitionSearchResultDto>>
{
    private readonly IRequisitionRepository _requisitionRepository;
    private readonly ICurrentUserService _currentUser;

    public SearchRequisitionsQueryHandler(IRequisitionRepository requisitionRepository, ICurrentUserService currentUser)
    {
        _requisitionRepository = requisitionRepository;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<RequisitionSearchResultDto>> Handle(SearchRequisitionsQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();
        var role = _currentUser.Role ?? throw new ForbiddenException();

        // Not a new access rule - the exact same data boundary Features 2/5/10 already established and
        // already enforce on GetMyRequisitionsQuery/GetDepartmentRequisitionsQuery/
        // GetProcurementRequisitionsQuery, generalized into one searchable query. Exactly one scope
        // applies per role; SystemAdmin gets none (unrestricted).
        Guid? ownerUserId = null;
        string? scopeDepartment = null;
        var scopeToPipeline = false;

        switch (role)
        {
            case UserRole.SystemAdmin:
                break;
            case UserRole.ProcurementOfficer:
                scopeToPipeline = true;
                break;
            case UserRole.DepartmentHead:
                scopeDepartment = _currentUser.Department ?? throw new ForbiddenException();
                break;
            default:
                ownerUserId = _currentUser.UserId ?? throw new ForbiddenException();
                break;
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var result = await _requisitionRepository.SearchAsync(
            companyId, ownerUserId, scopeDepartment, scopeToPipeline,
            request.FreeText, request.RequesterSearch, request.Statuses, request.Priority, request.Department,
            request.CategoryId, request.CategoryItemId, request.DateFrom, request.DateTo, request.NeedByFrom, request.NeedByTo,
            request.SortBy, request.SortDescending, page, pageSize, cancellationToken);

        return new PagedResult<RequisitionSearchResultDto>(
            result.Items.Select(RequisitionSearchResultDto.FromEntity).ToList(), result.TotalCount, result.Page, result.PageSize);
    }
}
