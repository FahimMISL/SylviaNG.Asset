using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.EligibilityPolicies.DTOs;
using RMS.Application.Interfaces;

namespace RMS.Application.Features.EligibilityPolicies.Queries.GetEligibilityPolicies;

public class GetEligibilityPoliciesQueryHandler : IRequestHandler<GetEligibilityPoliciesQuery, List<EligibilityPolicySummaryDto>>
{
    private readonly IEligibilityPolicyRepository _policyRepository;
    private readonly ICurrentUserService _currentUser;

    public GetEligibilityPoliciesQueryHandler(IEligibilityPolicyRepository policyRepository, ICurrentUserService currentUser)
    {
        _policyRepository = policyRepository;
        _currentUser = currentUser;
    }

    public async Task<List<EligibilityPolicySummaryDto>> Handle(GetEligibilityPoliciesQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();
        var policies = await _policyRepository.GetAllAsync(companyId, cancellationToken);

        var filtered = request.IsActive.HasValue ? policies.Where(p => p.IsActive == request.IsActive.Value) : policies;
        return filtered.Select(EligibilityPolicySummaryDto.FromEntity).ToList();
    }
}
