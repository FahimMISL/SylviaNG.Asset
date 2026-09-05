using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.EligibilityPolicies.Commands.PermanentlyDeleteEligibilityPolicy;

public class PermanentlyDeleteEligibilityPolicyCommandHandler : IRequestHandler<PermanentlyDeleteEligibilityPolicyCommand, Unit>
{
    private readonly IEligibilityPolicyRepository _policyRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public PermanentlyDeleteEligibilityPolicyCommandHandler(
        IEligibilityPolicyRepository policyRepository, ICurrentUserService currentUser, IAuditLogger auditLogger, IUnitOfWork unitOfWork)
    {
        _policyRepository = policyRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(PermanentlyDeleteEligibilityPolicyCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsInRole(UserRole.SystemAdmin))
        {
            throw new ForbiddenException();
        }

        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var policy = await _policyRepository.GetByIdIncludingDeletedAsync(companyId, request.PolicyId, cancellationToken)
            ?? throw new NotFoundException(nameof(EligibilityPolicy), request.PolicyId);

        if (!policy.IsDeleted)
        {
            throw new ConflictException("Only items already in Trash can be permanently deleted - move it to Trash first.");
        }

        var name = policy.Name;
        _policyRepository.Remove(policy);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            "EligibilityPolicyPermanentlyDeleted", nameof(EligibilityPolicy), policy.Id, $"Name={name}", cancellationToken);

        return Unit.Value;
    }
}
