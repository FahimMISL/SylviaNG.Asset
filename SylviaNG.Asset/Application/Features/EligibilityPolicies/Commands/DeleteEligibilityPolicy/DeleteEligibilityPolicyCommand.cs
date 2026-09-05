using MediatR;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.EligibilityPolicies.Commands.DeleteEligibilityPolicy;

/// <summary>Soft-delete (Trash), not a real removal - see EligibilityPolicy.Delete(). Recoverable via
/// RestoreEligibilityPolicyCommand until EligibilityPolicyTrashPurgeService's retention window elapses,
/// or immediately and permanently via PermanentlyDeleteEligibilityPolicyCommand (SystemAdmin only).
/// Feature 10: permission-guarded (Module=EligibilityPolicy, Action=Delete) - same permission also
/// gates viewing/restoring Trash, since restoring is the direct inverse of this same action.
///
/// IRequest&lt;Unit&gt;, not plain IRequest: MediatR 14 dispatches a bare `IRequest` command through a
/// completely separate internal path (ISender.Send&lt;TRequest&gt;(TRequest,ct), paired with
/// IRequestHandler&lt;TRequest&gt; - note the single generic parameter) that does NOT run
/// IPipelineBehavior&lt;TRequest,TResponse&gt; at all - confirmed live: a plain-IRequest version of this
/// exact command let a View-only user delete a policy outright, despite PermissionAuthorizationBehavior
/// existing and being correctly registered. IRequest&lt;Unit&gt; forces ISender.Send to bind to the
/// generic-response overload instead, which does run the pipeline. Every other permission-guarded
/// command in this codebase already returns a real DTO for this same reason (whether anyone realized
/// it at the time or not) - this was the only one that didn't.</summary>
public record DeleteEligibilityPolicyCommand(Guid PolicyId) : IRequest<Unit>, IPermissionGuardedRequest
{
    public PermissionModule Module => PermissionModule.EligibilityPolicy;
    public PermissionAction Action => PermissionAction.Delete;
}
