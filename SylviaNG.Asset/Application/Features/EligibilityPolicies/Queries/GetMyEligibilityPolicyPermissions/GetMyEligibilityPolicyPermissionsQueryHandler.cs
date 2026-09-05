using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.EligibilityPolicies.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.EligibilityPolicies.Queries.GetMyEligibilityPolicyPermissions;

public class GetMyEligibilityPolicyPermissionsQueryHandler
    : IRequestHandler<GetMyEligibilityPolicyPermissionsQuery, EligibilityPolicyMyPermissionsDto>
{
    private readonly IPermissionService _permissionService;
    private readonly ICurrentUserService _currentUser;

    public GetMyEligibilityPolicyPermissionsQueryHandler(IPermissionService permissionService, ICurrentUserService currentUser)
    {
        _permissionService = permissionService;
        _currentUser = currentUser;
    }

    public async Task<EligibilityPolicyMyPermissionsDto> Handle(
        GetMyEligibilityPolicyPermissionsQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();
        var role = _currentUser.Role ?? throw new ForbiddenException();

        const PermissionModule module = PermissionModule.EligibilityPolicy;

        var canView = await _permissionService.HasPermissionAsync(companyId, role, module, PermissionAction.View, cancellationToken);
        var canCreate = await _permissionService.HasPermissionAsync(companyId, role, module, PermissionAction.Create, cancellationToken);
        var canEdit = await _permissionService.HasPermissionAsync(companyId, role, module, PermissionAction.Edit, cancellationToken);
        var canDelete = await _permissionService.HasPermissionAsync(companyId, role, module, PermissionAction.Delete, cancellationToken);

        return new EligibilityPolicyMyPermissionsDto(canView, canCreate, canEdit, canDelete, role == UserRole.SystemAdmin);
    }
}
