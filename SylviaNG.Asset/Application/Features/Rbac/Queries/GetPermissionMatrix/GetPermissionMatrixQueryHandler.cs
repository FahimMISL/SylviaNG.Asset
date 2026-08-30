using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Rbac.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Rbac.Queries.GetPermissionMatrix;

public class GetPermissionMatrixQueryHandler : IRequestHandler<GetPermissionMatrixQuery, PermissionMatrixDto>
{
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly ICurrentUserService _currentUser;

    public GetPermissionMatrixQueryHandler(IRolePermissionRepository rolePermissionRepository, ICurrentUserService currentUser)
    {
        _rolePermissionRepository = rolePermissionRepository;
        _currentUser = currentUser;
    }

    public async Task<PermissionMatrixDto> Handle(GetPermissionMatrixQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var existing = (await _rolePermissionRepository.GetForRoleAsync(companyId, request.Role, cancellationToken))
            .ToDictionary(p => (p.Module, p.Action), p => p.IsAllowed);

        // Every (Module, Action) combination is listed, even ones with no row - matches
        // PermissionService's own fail-closed default (no row = not allowed) so the matrix UI never
        // shows something as granted that the actual enforcement would deny.
        var cells = new List<PermissionCellDto>();
        foreach (var module in Enum.GetValues<PermissionModule>())
        {
            foreach (var action in Enum.GetValues<PermissionAction>())
            {
                var isAllowed = request.Role == UserRole.SystemAdmin || existing.TryGetValue((module, action), out var allowed) && allowed;
                cells.Add(new PermissionCellDto(module, action, isAllowed));
            }
        }

        return new PermissionMatrixDto(request.Role, cells);
    }
}
