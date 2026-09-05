using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.EligibilityPolicies.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.EligibilityPolicies.Commands.RestoreEligibilityPolicy;

public class RestoreEligibilityPolicyCommandHandler : IRequestHandler<RestoreEligibilityPolicyCommand, EligibilityPolicyDto>
{
    private readonly IEligibilityPolicyRepository _policyRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public RestoreEligibilityPolicyCommandHandler(
        IEligibilityPolicyRepository policyRepository, ICurrentUserService currentUser, IAuditLogger auditLogger, IUnitOfWork unitOfWork)
    {
        _policyRepository = policyRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task<EligibilityPolicyDto> Handle(RestoreEligibilityPolicyCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var policy = await _policyRepository.GetByIdIncludingDeletedAsync(companyId, request.PolicyId, cancellationToken)
            ?? throw new NotFoundException(nameof(EligibilityPolicy), request.PolicyId);

        if (!policy.IsDeleted)
        {
            throw new ConflictException("This policy isn't in Trash.");
        }

        // Same at-most-one-active-per-scope guard Create/Update/SetActiveState already enforce -
        // another policy may have become the active one for this (Category, Item) while this one sat
        // in Trash, and restoring it as still-Active would recreate the exact routing ambiguity that
        // guard exists to prevent.
        if (policy.IsActive
            && await _policyRepository.ActivePolicyExistsAsync(companyId, policy.CategoryId, policy.CategoryItemId, policy.Id, cancellationToken))
        {
            throw new ConflictException(
                "An active eligibility policy already exists for this Category/Type combination. Restore this one as inactive, or deactivate the other first.");
        }

        policy.Restore();
        policy.UpdatedByUserId = _currentUser.UserId;
        policy.UpdatedAtUtc = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync("EligibilityPolicyRestored", nameof(EligibilityPolicy), policy.Id, $"Name={policy.Name}", cancellationToken);

        return EligibilityPolicyDto.FromEntity(policy);
    }
}
