using MediatR;
using RMS.Application.Features.EligibilityPolicies.DTOs;

namespace RMS.Application.Features.EligibilityPolicies.Queries.GetEligibilityPolicies;

public record GetEligibilityPoliciesQuery(bool? IsActive) : IRequest<List<EligibilityPolicySummaryDto>>;
