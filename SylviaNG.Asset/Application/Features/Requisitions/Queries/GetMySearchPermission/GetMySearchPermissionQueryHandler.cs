using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Requisitions.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Requisitions.Queries.GetMySearchPermission;

public class GetMySearchPermissionQueryHandler : IRequestHandler<GetMySearchPermissionQuery, SearchMyPermissionDto>
{
    private readonly IPermissionService _permissionService;
    private readonly ICurrentUserService _currentUser;

    public GetMySearchPermissionQueryHandler(IPermissionService permissionService, ICurrentUserService currentUser)
    {
        _permissionService = permissionService;
        _currentUser = currentUser;
    }

    public async Task<SearchMyPermissionDto> Handle(GetMySearchPermissionQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();
        var role = _currentUser.Role ?? throw new ForbiddenException();

        var canView = await _permissionService.HasPermissionAsync(companyId, role, PermissionModule.Search, PermissionAction.View, cancellationToken);

        return new SearchMyPermissionDto(canView, role == UserRole.SystemAdmin);
    }
}
