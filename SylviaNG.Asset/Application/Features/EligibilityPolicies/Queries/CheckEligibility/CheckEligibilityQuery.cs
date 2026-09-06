using MediatR;
using RMS.Application.Features.EligibilityPolicies.DTOs;

namespace RMS.Application.Features.EligibilityPolicies.Queries.CheckEligibility;

/// <summary>GET api/eligibility-policies/check?categoryId=&categoryItemId= - always evaluated for the
/// CURRENT authenticated user (see handler/ICurrentUserService), never a client-supplied user id.</summary>
public record CheckEligibilityQuery(Guid CategoryId, Guid? CategoryItemId) : IRequest<EligibilityCheckResultDto>;
