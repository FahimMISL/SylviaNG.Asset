using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.EligibilityPolicies.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.EligibilityPolicies.Queries.GetEligibilityPolicyById;

public class GetEligibilityPolicyByIdQueryHandler : IRequestHandler<GetEligibilityPolicyByIdQuery, EligibilityPolicyDto>
{
    private readonly IEligibilityPolicyRepository _policyRepository;
    private readonly ICurrentUserService _currentUser;

    public GetEligibilityPolicyByIdQueryHandler(IEligibilityPolicyRepository policyRepository, ICurrentUserService currentUser)
    {
        _policyRepository = policyRepository;
        _currentUser = currentUser;
    }

    public async Task<EligibilityPolicyDto> Handle(GetEligibilityPolicyByIdQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var policy = await _policyRepository.GetByIdAsync(companyId, request.PolicyId, cancellationToken)
            ?? throw new NotFoundException(nameof(EligibilityPolicy), request.PolicyId);
        return EligibilityPolicyDto.FromEntity(policy);
    }
}
