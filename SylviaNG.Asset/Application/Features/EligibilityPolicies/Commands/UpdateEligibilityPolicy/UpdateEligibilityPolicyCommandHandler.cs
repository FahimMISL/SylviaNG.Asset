using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.EligibilityPolicies.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.EligibilityPolicies.Commands.UpdateEligibilityPolicy;

public class UpdateEligibilityPolicyCommandHandler : IRequestHandler<UpdateEligibilityPolicyCommand, EligibilityPolicyDto>
{
    private readonly IEligibilityPolicyRepository _policyRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEligibilityPolicyCommandHandler(
        IEligibilityPolicyRepository policyRepository, ICategoryRepository categoryRepository,
        ICurrentUserService currentUser, IAuditLogger auditLogger, IUnitOfWork unitOfWork)
    {
        _policyRepository = policyRepository;
        _categoryRepository = categoryRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task<EligibilityPolicyDto> Handle(UpdateEligibilityPolicyCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var policy = await _policyRepository.GetByIdAsync(companyId, request.PolicyId, cancellationToken)
            ?? throw new NotFoundException(nameof(EligibilityPolicy), request.PolicyId);

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(RequisitionCategory), request.CategoryId);

        if (request.CategoryItemId.HasValue && category.Items.All(i => i.Id != request.CategoryItemId.Value))
        {
            throw new NotFoundException(nameof(CategoryItem), request.CategoryItemId.Value);
        }

        if (request.IsActive
            && await _policyRepository.ActivePolicyExistsAsync(companyId, request.CategoryId, request.CategoryItemId, policy.Id, cancellationToken))
        {
            throw new ConflictException(
                "An active eligibility policy already exists for this Category/Type combination. Deactivate it before activating a new one.");
        }

        policy.Name = request.Name;
        policy.Description = request.Description;
        policy.CategoryId = request.CategoryId;
        policy.CategoryItemId = request.CategoryItemId;
        policy.IsActive = request.IsActive;
        policy.UpdatedByUserId = _currentUser.UserId;
        policy.UpdatedAtUtc = DateTime.UtcNow;

        var newCriteria = request.Criteria
            .Select(c => new EligibilityPolicyCriterion { CriterionType = c.CriterionType, AllowedValue = c.AllowedValue })
            .ToList();
        _policyRepository.ReplaceCriteria(policy, newCriteria);

        var newRule = request.ReplacementRule is null
            ? null
            : new EligibilityPolicyReplacementRule { DurationValue = request.ReplacementRule.DurationValue, DurationUnit = request.ReplacementRule.DurationUnit };
        _policyRepository.SetReplacementRule(policy, newRule);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync("EligibilityPolicyUpdated", nameof(EligibilityPolicy), policy.Id, $"CategoryId={policy.CategoryId}", cancellationToken);

        var saved = await _policyRepository.GetByIdAsync(companyId, policy.Id, cancellationToken) ?? policy;
        return EligibilityPolicyDto.FromEntity(saved);
    }
}
