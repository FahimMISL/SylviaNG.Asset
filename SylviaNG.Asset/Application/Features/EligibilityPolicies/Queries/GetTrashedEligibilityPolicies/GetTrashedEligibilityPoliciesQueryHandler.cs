using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.EligibilityPolicies.DTOs;
using RMS.Application.Interfaces;

namespace RMS.Application.Features.EligibilityPolicies.Queries.GetTrashedEligibilityPolicies;

public class GetTrashedEligibilityPoliciesQueryHandler : IRequestHandler<GetTrashedEligibilityPoliciesQuery, List<EligibilityPolicyTrashDto>>
{
    private readonly IEligibilityPolicyRepository _policyRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;

    public GetTrashedEligibilityPoliciesQueryHandler(
        IEligibilityPolicyRepository policyRepository, IUserRepository userRepository, ICurrentUserService currentUser)
    {
        _policyRepository = policyRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task<List<EligibilityPolicyTrashDto>> Handle(GetTrashedEligibilityPoliciesQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var policies = await _policyRepository.GetTrashedAsync(companyId, cancellationToken);

        var deleterIds = policies.Where(p => p.DeletedByUserId.HasValue).Select(p => p.DeletedByUserId!.Value).Distinct().ToList();
        var deleters = deleterIds.Count > 0
            ? (await _userRepository.GetByIdsAsync(deleterIds, cancellationToken)).ToDictionary(u => u.Id, u => u.FullName)
            : new Dictionary<Guid, string>();

        return policies
            .Select(p => EligibilityPolicyTrashDto.FromEntity(
                p, p.DeletedByUserId.HasValue && deleters.TryGetValue(p.DeletedByUserId.Value, out var name) ? name : null))
            .ToList();
    }
}
