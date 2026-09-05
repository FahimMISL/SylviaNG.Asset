using MediatR;
using RMS.Application.Features.EligibilityPolicies.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.EligibilityPolicies.Queries.GetTrashedEligibilityPolicies;

/// <summary>Trash view - same permission as deleting (Module=EligibilityPolicy, Action=Delete), since
/// whoever can send a policy to Trash is exactly who should be able to see and restore it.</summary>
public record GetTrashedEligibilityPoliciesQuery : IRequest<List<EligibilityPolicyTrashDto>>, IPermissionGuardedRequest
{
    public PermissionModule Module => PermissionModule.EligibilityPolicy;
    public PermissionAction Action => PermissionAction.Delete;
}
