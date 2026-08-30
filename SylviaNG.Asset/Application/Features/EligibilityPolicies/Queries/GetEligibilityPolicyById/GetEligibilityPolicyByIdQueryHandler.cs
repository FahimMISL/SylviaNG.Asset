using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.EligibilityPolicies.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.EligibilityPolicies.Queries.GetEligibilityPolicyById;

public class GetEligibilityPolicyByIdQueryHandler : IRequestHandler<GetEligibilityPolicyByIdQuery, EligibilityPolicyDto>
{
    private readonly IEligibilityPolicyRepository _policyRepository;

    public GetEligibilityPolicyByIdQueryHandler(IEligibilityPolicyRepository policyRepository)
    {
        _policyRepository = policyRepository;
    }

    public async Task<EligibilityPolicyDto> Handle(GetEligibilityPolicyByIdQuery request, CancellationToken cancellationToken)
    {
        var policy = await _policyRepository.GetByIdAsync(request.PolicyId, cancellationToken)
            ?? throw new NotFoundException(nameof(EligibilityPolicy), request.PolicyId);
        return EligibilityPolicyDto.FromEntity(policy);
    }
}
