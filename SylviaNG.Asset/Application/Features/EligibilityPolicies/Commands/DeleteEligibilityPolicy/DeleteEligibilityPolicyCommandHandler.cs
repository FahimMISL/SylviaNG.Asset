using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.EligibilityPolicies.Commands.DeleteEligibilityPolicy;

public class DeleteEligibilityPolicyCommandHandler : IRequestHandler<DeleteEligibilityPolicyCommand>
{
    private readonly IEligibilityPolicyRepository _policyRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteEligibilityPolicyCommandHandler(
        IEligibilityPolicyRepository policyRepository, ICurrentUserService currentUser, IAuditLogger auditLogger, IUnitOfWork unitOfWork)
    {
        _policyRepository = policyRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteEligibilityPolicyCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var policy = await _policyRepository.GetByIdAsync(companyId, request.PolicyId, cancellationToken)
            ?? throw new NotFoundException(nameof(EligibilityPolicy), request.PolicyId);

        _policyRepository.Remove(policy);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync("EligibilityPolicyDeleted", nameof(EligibilityPolicy), policy.Id, $"Name={policy.Name}", cancellationToken);
    }
}
