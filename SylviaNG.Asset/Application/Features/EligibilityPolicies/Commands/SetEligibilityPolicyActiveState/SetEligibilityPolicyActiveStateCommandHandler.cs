using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.EligibilityPolicies.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.EligibilityPolicies.Commands.SetEligibilityPolicyActiveState;

public class SetEligibilityPolicyActiveStateCommandHandler : IRequestHandler<SetEligibilityPolicyActiveStateCommand, EligibilityPolicyDto>
{
    private readonly IEligibilityPolicyRepository _policyRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public SetEligibilityPolicyActiveStateCommandHandler(
        IEligibilityPolicyRepository policyRepository, ICurrentUserService currentUser, IAuditLogger auditLogger, IUnitOfWork unitOfWork)
    {
        _policyRepository = policyRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task<EligibilityPolicyDto> Handle(SetEligibilityPolicyActiveStateCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var policy = await _policyRepository.GetByIdAsync(request.PolicyId, cancellationToken)
            ?? throw new NotFoundException(nameof(EligibilityPolicy), request.PolicyId);
        if (policy.CompanyId != companyId)
        {
            throw new ForbiddenException();
        }

        if (request.IsActive)
        {
            // Same at-most-one-active-per-scope guard as Create/Update - reactivating a policy from
            // the list view must not silently create the same resolution ambiguity either.
            if (await _policyRepository.ActivePolicyExistsAsync(companyId, policy.CategoryId, policy.CategoryItemId, policy.Id, cancellationToken))
            {
                throw new ConflictException(
                    "An active eligibility policy already exists for this Category/Type combination. Deactivate it before activating this one.");
            }

            policy.Activate();
        }
        else
        {
            policy.Deactivate();
        }

        policy.UpdatedByUserId = _currentUser.UserId;
        policy.UpdatedAtUtc = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            request.IsActive ? "EligibilityPolicyActivated" : "EligibilityPolicyDeactivated", nameof(EligibilityPolicy), policy.Id, null, cancellationToken);

        return EligibilityPolicyDto.FromEntity(policy);
    }
}
