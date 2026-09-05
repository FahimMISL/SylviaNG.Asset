using MediatR;
using RMS.Application.Features.EligibilityPolicies.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.EligibilityPolicies.Commands.RestoreEligibilityPolicy;

/// <summary>Pulls a policy back out of Trash - the direct inverse of DeleteEligibilityPolicyCommand,
/// same permission (Module=EligibilityPolicy, Action=Delete). Only works on a policy that's currently
/// in Trash (IsDeleted=true); its prior IsActive value is left exactly as it was when deleted.</summary>
public record RestoreEligibilityPolicyCommand(Guid PolicyId) : IRequest<EligibilityPolicyDto>, IPermissionGuardedRequest
{
    public PermissionModule Module => PermissionModule.EligibilityPolicy;
    public PermissionAction Action => PermissionAction.Delete;
}
