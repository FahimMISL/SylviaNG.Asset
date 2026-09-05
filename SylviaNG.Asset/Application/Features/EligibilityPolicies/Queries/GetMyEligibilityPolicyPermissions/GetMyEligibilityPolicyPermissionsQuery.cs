using MediatR;
using RMS.Application.Features.EligibilityPolicies.DTOs;

namespace RMS.Application.Features.EligibilityPolicies.Queries.GetMyEligibilityPolicyPermissions;

/// <summary>Not itself permission-guarded - it's a self-check ("what can I do here"), same class of
/// endpoint as GET /check (always evaluates the CALLER, never a client-supplied user/role). The
/// Eligibility &amp; Policies screen uses this to show/hide New/Edit/Activate/Delete/Restore/Permanent
/// Delete correctly for whoever's looking, instead of guessing from their role name.</summary>
public record GetMyEligibilityPolicyPermissionsQuery : IRequest<EligibilityPolicyMyPermissionsDto>;
