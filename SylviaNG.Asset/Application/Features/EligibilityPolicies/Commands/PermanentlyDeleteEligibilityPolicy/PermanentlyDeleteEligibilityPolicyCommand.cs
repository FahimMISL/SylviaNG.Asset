using MediatR;

namespace RMS.Application.Features.EligibilityPolicies.Commands.PermanentlyDeleteEligibilityPolicy;

/// <summary>Real, irreversible removal - unlike DeleteEligibilityPolicyCommand (which only moves a
/// policy to Trash), this actually deletes the row and cannot be undone. Deliberately NOT
/// IPermissionGuardedRequest: per the feature's own design, Permanent Delete is SystemAdmin-only
/// regardless of what the Roles &amp; Permissions matrix grants for EligibilityPolicy/Delete to any
/// other role - the handler enforces this with a direct role check, same pattern
/// NotificationTemplates' admin-only commands already use.
///
/// IRequest&lt;Unit&gt;, not plain IRequest: this command's own authorization doesn't depend on the
/// MediatR pipeline (it's an inline IsInRole check inside the handler, unaffected either way), but a
/// bare `IRequest` command is dispatched by MediatR 14 through a path that skips
/// IPipelineBehavior&lt;TRequest,TResponse&gt; entirely - see DeleteEligibilityPolicyCommand's remarks
/// for the confirmed bug that caused elsewhere. Kept consistent here so nothing in this file could
/// silently stop being enforced if this command ever gains an IPermissionGuardedRequest check later.</summary>
public record PermanentlyDeleteEligibilityPolicyCommand(Guid PolicyId) : IRequest<Unit>;
