using MediatR;
using RMS.Application.Features.EligibilityPolicies.DTOs;

namespace RMS.Application.Features.EligibilityPolicies.Queries.GetEligibilityPolicyById;

public record GetEligibilityPolicyByIdQuery(Guid PolicyId) : IRequest<EligibilityPolicyDto>;
