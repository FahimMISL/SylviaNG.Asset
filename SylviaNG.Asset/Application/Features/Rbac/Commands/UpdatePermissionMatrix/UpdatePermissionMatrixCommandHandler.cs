using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Rbac.Commands.UpdatePermissionMatrix;

public class UpdatePermissionMatrixCommandHandler : IRequestHandler<UpdatePermissionMatrixCommand>
{
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePermissionMatrixCommandHandler(
        IRolePermissionRepository rolePermissionRepository, ICurrentUserService currentUser, IAuditLogger auditLogger, IUnitOfWork unitOfWork)
    {
        _rolePermissionRepository = rolePermissionRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdatePermissionMatrixCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        // SystemAdmin implicitly passes every permission check (PermissionService never even consults
        // this table for it) - editing its row would look like it does something without actually
        // changing any real enforcement, so it's rejected outright rather than left misleadingly
        // editable. This is also what makes "can't lock yourself out" true by construction, not by a
        // special-case guard here.
        if (request.Role == UserRole.SystemAdmin)
        {
            throw new ConflictException("SystemAdmin implicitly has every permission and cannot be edited.");
        }

        var changed = new List<string>();
        foreach (var cell in request.Permissions)
        {
            var existing = await _rolePermissionRepository.GetAsync(companyId, request.Role, cell.Module, cell.Action, cancellationToken);
            if (existing is not null)
            {
                if (existing.IsAllowed != cell.IsAllowed)
                {
                    changed.Add($"{cell.Module}/{cell.Action}: {existing.IsAllowed} -> {cell.IsAllowed}");
                    existing.IsAllowed = cell.IsAllowed;
                }
            }
            else if (cell.IsAllowed)
            {
                // No row already means denied (PermissionService's fail-closed default) - only
                // materialize a row here when it's actually granting something. Without this guard,
                // the matrix UI's Save (which always round-trips the full 66-cell grid, not just the
                // cells the admin touched) would insert a row for every untouched false cell on every
                // save, silently ballooning this table without changing any real enforcement.
                _rolePermissionRepository.Add(new RolePermission
                {
                    CompanyId = companyId,
                    Role = request.Role,
                    Module = cell.Module,
                    Action = cell.Action,
                    IsAllowed = true,
                });
                changed.Add($"{cell.Module}/{cell.Action}: false -> true");
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (changed.Count > 0)
        {
            await _auditLogger.LogAsync(
                "PermissionMatrixUpdated", nameof(RolePermission), Guid.Empty,
                $"Role={request.Role}; Changes: {string.Join(", ", changed)}", cancellationToken);
        }
    }
}
