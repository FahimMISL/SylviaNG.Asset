using FluentAssertions;
using Moq;
using RMS.Application.Features.EligibilityPolicies.Services;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace SylviaNG.Assets.Tests.Services;

/// <summary>Feature 4: PolicyEvaluationService's resolution precedence, AND/OR criterion semantics,
/// and replacement-rule (waiting period) evaluation.</summary>
public class PolicyEvaluationServiceTests
{
    private readonly Mock<IEligibilityPolicyRepository> _policyRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRequisitionRepository> _requisitionRepository = new();
    private readonly PolicyEvaluationService _service;

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _categoryId = Guid.NewGuid();
    private readonly Guid _categoryItemId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public PolicyEvaluationServiceTests()
    {
        _service = new PolicyEvaluationService(_policyRepository.Object, _userRepository.Object, _requisitionRepository.Object);
    }

    private User NewUser(string? grade = "Officer", string? department = "IT") => new()
    {
        Id = _userId,
        CompanyId = _companyId,
        FullName = "Emma Employee",
        Grade = grade,
        Department = department,
        EmploymentType = EmploymentType.Permanent,
    };

    private EligibilityPolicy NewPolicy(Guid? categoryItemId, params EligibilityPolicyCriterion[] criteria)
    {
        var policy = new EligibilityPolicy { CompanyId = _companyId, CategoryId = _categoryId, CategoryItemId = categoryItemId, IsActive = true };
        policy.Criteria.AddRange(criteria);
        return policy;
    }

    [Fact]
    public async Task NoPolicyConfigured_ReturnsOpenAccess()
    {
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(NewUser());
        _policyRepository
            .Setup(r => r.GetResolvablePolicyAsync(_companyId, _categoryId, _categoryItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EligibilityPolicy?)null);

        var result = await _service.CheckAsync(_userId, _categoryId, _categoryItemId, CancellationToken.None);

        result.IsEligible.Should().BeTrue();
        result.Reason.Should().BeNull();
    }

    [Fact]
    public async Task MatchingGradeAndDepartment_IsEligible()
    {
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(NewUser(grade: "Officer", department: "IT"));
        var policy = NewPolicy(
            _categoryItemId,
            new EligibilityPolicyCriterion { CriterionType = EligibilityCriterionType.Grade, AllowedValue = "Officer" },
            new EligibilityPolicyCriterion { CriterionType = EligibilityCriterionType.Department, AllowedValue = "IT" });
        _policyRepository
            .Setup(r => r.GetResolvablePolicyAsync(_companyId, _categoryId, _categoryItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);

        var result = await _service.CheckAsync(_userId, _categoryId, _categoryItemId, CancellationToken.None);

        result.IsEligible.Should().BeTrue();
    }

    [Fact]
    public async Task WrongGrade_IsBlocked_WithReasonMentioningGrade()
    {
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(NewUser(grade: "Officer"));
        var policy = NewPolicy(_categoryItemId, new EligibilityPolicyCriterion { CriterionType = EligibilityCriterionType.Grade, AllowedValue = "Manager" });
        _policyRepository
            .Setup(r => r.GetResolvablePolicyAsync(_companyId, _categoryId, _categoryItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);

        var result = await _service.CheckAsync(_userId, _categoryId, _categoryItemId, CancellationToken.None);

        result.IsEligible.Should().BeFalse();
        result.Reason.Should().Contain("Grade");
    }

    [Fact]
    public async Task WrongDepartment_IsBlocked_WithReasonMentioningDepartment()
    {
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(NewUser(department: "Finance"));
        var policy = NewPolicy(_categoryItemId, new EligibilityPolicyCriterion { CriterionType = EligibilityCriterionType.Department, AllowedValue = "IT" });
        _policyRepository
            .Setup(r => r.GetResolvablePolicyAsync(_companyId, _categoryId, _categoryItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);

        var result = await _service.CheckAsync(_userId, _categoryId, _categoryItemId, CancellationToken.None);

        result.IsEligible.Should().BeFalse();
        result.Reason.Should().Contain("Department");
    }

    [Fact]
    public async Task TwoCriteriaOfSameType_AreOrd_EitherAllowedValueMatches()
    {
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(NewUser(department: "Finance"));
        var policy = NewPolicy(
            _categoryItemId,
            new EligibilityPolicyCriterion { CriterionType = EligibilityCriterionType.Department, AllowedValue = "IT" },
            new EligibilityPolicyCriterion { CriterionType = EligibilityCriterionType.Department, AllowedValue = "Finance" });
        _policyRepository
            .Setup(r => r.GetResolvablePolicyAsync(_companyId, _categoryId, _categoryItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);

        var result = await _service.CheckAsync(_userId, _categoryId, _categoryItemId, CancellationToken.None);

        result.IsEligible.Should().BeTrue();
    }

    /// <summary>Repository resolution itself already implements the category-level fallback -
    /// exercising it end to end here (item id null, only a category-scoped policy resolvable).</summary>
    [Fact]
    public async Task CategoryLevelPolicy_AppliesWhenNoItemSpecified()
    {
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(NewUser(grade: "Officer"));
        var categoryPolicy = NewPolicy(null, new EligibilityPolicyCriterion { CriterionType = EligibilityCriterionType.Grade, AllowedValue = "Manager" });
        _policyRepository
            .Setup(r => r.GetResolvablePolicyAsync(_companyId, _categoryId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(categoryPolicy);

        var result = await _service.CheckAsync(_userId, _categoryId, null, CancellationToken.None);

        result.IsEligible.Should().BeFalse("the category-level policy requires Manager grade and the user is only an Officer");
    }

    /// <summary>Resolution precedence: an item-specific policy is what GetResolvablePolicyAsync
    /// returns whenever one exists for that exact item, regardless of a category-level policy also
    /// existing - PolicyEvaluationService only ever evaluates whatever the repository resolved.</summary>
    [Fact]
    public async Task ItemLevelPolicy_IsWhatGetsEvaluated_WhenResolverReturnsIt()
    {
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(NewUser(grade: "Officer"));
        var itemPolicy = NewPolicy(_categoryItemId, new EligibilityPolicyCriterion { CriterionType = EligibilityCriterionType.Grade, AllowedValue = "Officer" });
        _policyRepository
            .Setup(r => r.GetResolvablePolicyAsync(_companyId, _categoryId, _categoryItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(itemPolicy);

        var result = await _service.CheckAsync(_userId, _categoryId, _categoryItemId, CancellationToken.None);

        result.IsEligible.Should().BeTrue("the item-level policy (Officer allowed) is the one resolved for this exact item, overriding any category-level policy");
    }

    [Fact]
    public async Task NoPriorRequisition_FirstTimeRequestIsAllowed()
    {
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(NewUser());
        var policy = NewPolicy(_categoryItemId);
        policy.ReplacementRule = new EligibilityPolicyReplacementRule { DurationValue = 24, DurationUnit = EligibilityDurationUnit.Months };
        _policyRepository
            .Setup(r => r.GetResolvablePolicyAsync(_companyId, _categoryId, _categoryItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);
        _requisitionRepository
            .Setup(r => r.GetMostRecentApprovedTransitionUtcAsync(
                _userId, _categoryId, _categoryItemId, It.IsAny<List<RequisitionStatus>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTime?)null);

        var result = await _service.CheckAsync(_userId, _categoryId, _categoryItemId, CancellationToken.None);

        result.IsEligible.Should().BeTrue();
    }

    [Fact]
    public async Task ReplacementPeriodStillActive_IsBlocked_WithNextEligibleDate()
    {
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(NewUser());
        var policy = NewPolicy(_categoryItemId);
        policy.ReplacementRule = new EligibilityPolicyReplacementRule { DurationValue = 24, DurationUnit = EligibilityDurationUnit.Months };
        _policyRepository
            .Setup(r => r.GetResolvablePolicyAsync(_companyId, _categoryId, _categoryItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);
        var approvedOneMonthAgo = DateTime.UtcNow.AddMonths(-1);
        _requisitionRepository
            .Setup(r => r.GetMostRecentApprovedTransitionUtcAsync(
                _userId, _categoryId, _categoryItemId, It.IsAny<List<RequisitionStatus>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(approvedOneMonthAgo);

        var result = await _service.CheckAsync(_userId, _categoryId, _categoryItemId, CancellationToken.None);

        result.IsEligible.Should().BeFalse();
        result.NextEligibleDateUtc.Should().BeCloseTo(approvedOneMonthAgo.AddMonths(24), TimeSpan.FromSeconds(5));
        result.Reason.Should().Contain("not currently eligible");
    }

    [Fact]
    public async Task ReplacementPeriodExpired_IsAllowed()
    {
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(NewUser());
        var policy = NewPolicy(_categoryItemId);
        policy.ReplacementRule = new EligibilityPolicyReplacementRule { DurationValue = 12, DurationUnit = EligibilityDurationUnit.Months };
        _policyRepository
            .Setup(r => r.GetResolvablePolicyAsync(_companyId, _categoryId, _categoryItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);
        var approvedTwoYearsAgo = DateTime.UtcNow.AddYears(-2);
        _requisitionRepository
            .Setup(r => r.GetMostRecentApprovedTransitionUtcAsync(
                _userId, _categoryId, _categoryItemId, It.IsAny<List<RequisitionStatus>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(approvedTwoYearsAgo);

        var result = await _service.CheckAsync(_userId, _categoryId, _categoryItemId, CancellationToken.None);

        result.IsEligible.Should().BeTrue();
    }

    /// <summary>A rejected/cancelled/draft/sent-back/submitted/under-review prior requisition must
    /// never count toward the restriction, not even as a "no prior request" fallback - the repository
    /// query itself is scoped to only the statuses that actually count (see
    /// IRequisitionRepository.GetMostRecentApprovedTransitionUtcAsync's remarks); this asserts the
    /// service passes exactly that qualifying set and treats a null result (e.g. the only prior
    /// requisition was Rejected, so nothing qualifies) as first-time-allowed.</summary>
    [Fact]
    public async Task RejectedPriorRequest_DoesNotCountTowardRestriction()
    {
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(NewUser());
        var policy = NewPolicy(_categoryItemId);
        policy.ReplacementRule = new EligibilityPolicyReplacementRule { DurationValue = 24, DurationUnit = EligibilityDurationUnit.Months };
        _policyRepository
            .Setup(r => r.GetResolvablePolicyAsync(_companyId, _categoryId, _categoryItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);

        List<RequisitionStatus>? capturedStatuses = null;
        _requisitionRepository
            .Setup(r => r.GetMostRecentApprovedTransitionUtcAsync(
                _userId, _categoryId, _categoryItemId, It.IsAny<List<RequisitionStatus>>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, Guid?, List<RequisitionStatus>, CancellationToken>((_, _, _, statuses, _) => capturedStatuses = statuses)
            .ReturnsAsync((DateTime?)null); // the user's only prior requisition for this item was Rejected - nothing qualifies.

        var result = await _service.CheckAsync(_userId, _categoryId, _categoryItemId, CancellationToken.None);

        result.IsEligible.Should().BeTrue();
        capturedStatuses.Should().NotBeNull();
        capturedStatuses.Should().NotContain(RequisitionStatus.Rejected);
        capturedStatuses.Should().NotContain(RequisitionStatus.Cancelled);
        capturedStatuses.Should().NotContain(RequisitionStatus.Draft);
        capturedStatuses.Should().NotContain(RequisitionStatus.SentBack);
        capturedStatuses.Should().NotContain(RequisitionStatus.Submitted);
        capturedStatuses.Should().NotContain(RequisitionStatus.UnderReview);
        capturedStatuses.Should().Contain(RequisitionStatus.Approved);
        capturedStatuses.Should().Contain(RequisitionStatus.PartiallyApproved);
    }
}
