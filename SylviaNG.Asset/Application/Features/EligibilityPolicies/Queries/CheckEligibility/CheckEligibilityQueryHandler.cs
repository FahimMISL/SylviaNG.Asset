using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.EligibilityPolicies.DTOs;
using RMS.Application.Features.EligibilityPolicies.Services;
using RMS.Application.Interfaces;

namespace RMS.Application.Features.EligibilityPolicies.Queries.CheckEligibility;

public class CheckEligibilityQueryHandler : IRequestHandler<CheckEligibilityQuery, EligibilityCheckResultDto>
{
    private readonly PolicyEvaluationService _policyEvaluationService;
    private readonly ICurrentUserService _currentUser;

    public CheckEligibilityQueryHandler(PolicyEvaluationService policyEvaluationService, ICurrentUserService currentUser)
    {
        _policyEvaluationService = policyEvaluationService;
        _currentUser = currentUser;
    }

    public Task<EligibilityCheckResultDto> Handle(CheckEligibilityQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();
        return _policyEvaluationService.CheckAsync(userId, request.CategoryId, request.CategoryItemId, cancellationToken);
    }
}
