using MediatR;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Rbac.Commands.UpdatePermissionMatrix;

public record PermissionCellInput(PermissionModule Module, PermissionAction Action, bool IsAllowed);

/// <summary>IRequest&lt;Unit&gt;, not plain IRequest: MediatR 14 dispatches a bare `IRequest` command
/// through a separate internal path that does NOT run IPipelineBehavior&lt;TRequest,TResponse&gt; at
/// all - confirmed via an identically-shaped bug in DeleteEligibilityPolicyCommand, where a
/// plain-IRequest command let its permission check be silently skipped entirely. This command editing
/// the permission matrix itself made that the single most critical instance of the bug in this
/// codebase - IRequest&lt;Unit&gt; forces ISender.Send onto the generic-response overload, which does
/// run the pipeline.</summary>
public record UpdatePermissionMatrixCommand(UserRole Role, List<PermissionCellInput> Permissions) : IRequest<Unit>, IPermissionGuardedRequest
{
    public PermissionModule Module => PermissionModule.Rbac;
    public PermissionAction Action => PermissionAction.Edit;
}
