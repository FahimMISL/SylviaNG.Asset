using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.EligibilityPolicies.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.EligibilityPolicies.Commands.CreateEligibilityPolicy;

public class CreateEligibilityPolicyCommandHandler : IRequestHandler<CreateEligibilityPolicyCommand, EligibilityPolicyDto>
{
    private readonly IEligibilityPolicyRepository _policyRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public CreateEligibilityPolicyCommandHandler(
        IEligibilityPolicyRepository policyRepository, ICategoryRepository categoryRepository,
        ICurrentUserService currentUser, IAuditLogger auditLogger, IUnitOfWork unitOfWork)
    {
        _policyRepository = policyRepository;
        _categoryRepository = categoryRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task<EligibilityPolicyDto> Handle(CreateEligibilityPolicyCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(RequisitionCategory), request.CategoryId);

        if (request.CategoryItemId.HasValue && category.Items.All(i => i.Id != request.CategoryItemId.Value))
        {
            throw new NotFoundException(nameof(CategoryItem), request.CategoryItemId.Value);
        }

        // At most one ACTIVE policy per (CompanyId, CategoryId, CategoryItemId) - see the Feature 4
        // plan's remarks referencing Feature 3's real "two active workflows both matched" bug.
        if (request.IsActive
            && await _policyRepository.ActivePolicyExistsAsync(companyId, request.CategoryId, request.CategoryItemId, null, cancellationToken))
        {
            throw new ConflictException(
                "An active eligibility policy already exists for this Category/Type combination. Deactivate it before activating a new one.");
        }

        var policy = new EligibilityPolicy
        {
            CompanyId = companyId,
            Name = request.Name,
            Description = request.Description,
            CategoryId = request.CategoryId,
            CategoryItemId = request.CategoryItemId,
            IsActive = request.IsActive,
            CreatedByUserId = _currentUser.UserId,
            CreatedAtUtc = DateTime.UtcNow,
        };

        foreach (var criterion in request.Criteria)
        {
            policy.Criteria.Add(new EligibilityPolicyCriterion
            {
                EligibilityPolicyId = policy.Id,
                CriterionType = criterion.CriterionType,
                AllowedValue = criterion.AllowedValue,
            });
        }

        if (request.ReplacementRule is not null)
        {
            policy.ReplacementRule = new EligibilityPolicyReplacementRule
            {
                EligibilityPolicyId = policy.Id,
                DurationValue = request.ReplacementRule.DurationValue,
                DurationUnit = request.ReplacementRule.DurationUnit,
            };
        }

        _policyRepository.Add(policy);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync("EligibilityPolicyCreated", nameof(EligibilityPolicy), policy.Id, $"CategoryId={policy.CategoryId}", cancellationToken);

        var saved = await _policyRepository.GetByIdAsync(companyId, policy.Id, cancellationToken) ?? policy;
        return EligibilityPolicyDto.FromEntity(saved);
    }
}
