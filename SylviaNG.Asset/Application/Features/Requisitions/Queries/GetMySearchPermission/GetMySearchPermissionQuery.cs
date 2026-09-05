using MediatR;
using RMS.Application.Features.Requisitions.DTOs;

namespace RMS.Application.Features.Requisitions.Queries.GetMySearchPermission;

/// <summary>Feature 11.5: not itself IPermissionGuardedRequest - a self-check has to be reachable by
/// someone who might not have the permission it's checking, same as GetMyEligibilityPolicyPermissionsQuery.</summary>
public record GetMySearchPermissionQuery : IRequest<SearchMyPermissionDto>;
