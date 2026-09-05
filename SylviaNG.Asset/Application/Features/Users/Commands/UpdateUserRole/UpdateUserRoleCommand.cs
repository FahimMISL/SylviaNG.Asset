using MediatR;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Users.Commands.UpdateUserRole;

/// <summary>Feature 10 (US-031): the one new write capability this feature adds to the previously
/// read-only Users directory - reassigning who has which of the 7 fixed roles.
///
/// IRequest&lt;Unit&gt;, not plain IRequest: MediatR 14 dispatches a bare `IRequest` command through a
/// separate internal path (paired with IRequestHandler&lt;TRequest&gt;, one generic parameter) that
/// does NOT run IPipelineBehavior&lt;TRequest,TResponse&gt; - confirmed via an identically-shaped bug in
/// DeleteEligibilityPolicyCommand, where a plain-IRequest version let a View-only user act with no
/// permission check at all despite PermissionAuthorizationBehavior being registered and correctly
/// blocking every IRequest&lt;TResponse&gt; command in this codebase. IRequest&lt;Unit&gt; forces
/// ISender.Send to bind to the generic-response overload instead, which does run the pipeline.</summary>
public record UpdateUserRoleCommand(Guid UserId, UserRole NewRole) : IRequest<Unit>, IPermissionGuardedRequest
{
    public PermissionModule Module => PermissionModule.Rbac;
    public PermissionAction Action => PermissionAction.Edit;
}
