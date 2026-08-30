using MediatR;
using RMS.Application.Features.EligibilityPolicies.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.EligibilityPolicies.Queries.GetEligibilityPolicies;

/// <summary>Feature 10: permission-guarded (Module=EligibilityPolicy, Action=View) - previously had no
/// role check at all, see the plan's Context.</summary>
public record GetEligibilityPoliciesQuery(bool? IsActive) : IRequest<List<EligibilityPolicySummaryDto>>, IPermissionGuardedRequest
{
    public PermissionModule Module => PermissionModule.EligibilityPolicy;
    public PermissionAction Action => PermissionAction.View;
}
