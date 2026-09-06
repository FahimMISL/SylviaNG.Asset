using FluentAssertions;
using Moq;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.ApprovalWorkflows.Services;
using RMS.Application.Features.EligibilityPolicies.Services;
using RMS.Application.Features.Requisitions.Commands.CreateRequisition;
using RMS.Application.Features.Requisitions.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace SylviaNG.Assets.Tests.Handlers;

/// <summary>
/// Feature 4: the backend-authoritative eligibility re-check wired into CreateRequisitionCommandHandler
/// (and, identically, UpdateRequisitionCommandHandler - both submission paths get the same gate, the
/// same "two submission paths" class of fix Feature 3 needed). A Draft save must never be blocked by a
/// policy; a real Submit must be, even if an earlier/frontend-only check was somehow bypassed.
/// </summary>
public class RequisitionEligibilityGateTests
{
    private readonly Mock<IRequisitionRepository> _requisitionRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IEligibilityPolicyRepository> _policyRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IApprovalWorkflowRepository> _workflowRepository = new();
    private readonly Mock<IRequisitionApprovalRepository> _requisitionApprovalRepository = new();
    private readonly Mock<INotificationService> _notificationService = new();

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _categoryId = Guid.NewGuid();
    private readonly Guid _categoryItemId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private readonly PolicyEvaluationService _policyEvaluationService;
    private readonly ApprovalWorkflowEngine _approvalWorkflowEngine;
    private readonly CreateRequisitionCommandHandler _handler;

    public RequisitionEligibilityGateTests()
    {
        _currentUser.Setup(c => c.CompanyId).Returns(_companyId);
        _currentUser.Setup(c => c.UserId).Returns(_userId);
        _currentUser.Setup(c => c.FullName).Returns("Emma Employee");
        _currentUser.Setup(c => c.Role).Returns(UserRole.Employee);

        var category = new RequisitionCategory { Id = _categoryId, CompanyId = _companyId, IsActive = true, CurrentVersionNumber = 1 };
        category.Items.Add(new CategoryItem { Id = _categoryItemId, Name = "Laptop", IsActive = true });
        _categoryRepository.Setup(r => r.GetByIdAsync(_categoryId, It.IsAny<CancellationToken>())).ReturnsAsync(category);

        // A policy that this requestor fails (wrong Grade) - real PolicyEvaluationService instance,
        // same convention as ApprovalWorkflowEngine being constructed for real (not mocked) in tests
        // that exercise a concrete engine class rather than an interface.
        var policy = new EligibilityPolicy { CompanyId = _companyId, CategoryId = _categoryId, CategoryItemId = _categoryItemId, IsActive = true };
        policy.Criteria.Add(new EligibilityPolicyCriterion { CriterionType = EligibilityCriterionType.Grade, AllowedValue = "Manager" });
        _policyRepository
            .Setup(r => r.GetResolvablePolicyAsync(_companyId, _categoryId, _categoryItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);
        _userRepository
            .Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = _userId, CompanyId = _companyId, Grade = "Officer" });

        _policyEvaluationService = new PolicyEvaluationService(_policyRepository.Object, _userRepository.Object, _requisitionRepository.Object);
        _approvalWorkflowEngine = new ApprovalWorkflowEngine(
            _workflowRepository.Object, _requisitionApprovalRepository.Object, _requisitionRepository.Object, _userRepository.Object);

        _handler = new CreateRequisitionCommandHandler(
            _requisitionRepository.Object, _categoryRepository.Object, _currentUser.Object, _auditLogger.Object, _unitOfWork.Object,
            _approvalWorkflowEngine, _policyEvaluationService, _notificationService.Object);
    }

    private CreateRequisitionCommand BuildCommand(bool submit) => new(
        _categoryId,
        [new RequisitionItemInput(_categoryItemId, 1)],
        RequisitionPriority.Medium,
        DateTime.UtcNow.AddDays(7),
        0,
        null,
        null,
        null,
        null,
        [],
        submit);

    [Fact]
    public async Task SaveAsDraft_IsNeverBlockedByAnIneligibilityPolicy()
    {
        _requisitionRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Requisition?)null);

        var act = () => _handler.Handle(BuildCommand(submit: false), CancellationToken.None);

        await act.Should().NotThrowAsync();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        // The policy repository must never even be consulted on a Draft save - draft saves are always
        // allowed regardless of eligibility, per the spec.
        _policyRepository.Verify(
            r => r.GetResolvablePolicyAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Submit_ReChecksEligibilityServerSide_AndBlocksWithConflictException_RegardlessOfAnyEarlierClientCheck()
    {
        var act = () => _handler.Handle(BuildCommand(submit: true), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ConflictException>();
        exception.Which.Message.Should().Contain("Grade");

        // Nothing should have been persisted - the gate runs before SaveChangesAsync.
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
