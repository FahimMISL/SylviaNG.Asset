using FluentAssertions;
using Moq;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.ApprovalWorkflows.Services;
using RMS.Application.Features.EligibilityPolicies.Services;
using RMS.Application.Features.Requisitions.Commands.UpdateRequisition;
using RMS.Application.Features.Requisitions.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace SylviaNG.Assets.Tests.Handlers;

/// <summary>
/// Feature 8 coverage for UpdateRequisitionCommandHandler: (1) the extended BuildDiff helper now
/// tracks Items changes in the "RequisitionAmended" audit trail (previously only scalar fields like
/// Priority/EstimatedCost were tracked - an item/quantity change on a SentBack amendment left no
/// trace at all); (2) the eligibility-blocked path mirrors CreateRequisitionCommandHandler's -
/// re-ordered so the new EligibilityCheckBlocked log call runs before any mutation of the already-
/// tracked entity, so a blocked resubmit still leaves no partial state (see RequisitionEligibilityGateTests
/// for the equivalent Create-side coverage).
/// </summary>
public class UpdateRequisitionAuditTests
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
    private readonly Guid _laptopId = Guid.NewGuid();
    private readonly Guid _monitorId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _requisitionId = Guid.NewGuid();

    private RequisitionCategory BuildCategory()
    {
        var category = new RequisitionCategory { Id = _categoryId, CompanyId = _companyId, IsActive = true, CurrentVersionNumber = 1 };
        category.Items.Add(new CategoryItem { Id = _laptopId, Name = "Laptop", IsActive = true });
        category.Items.Add(new CategoryItem { Id = _monitorId, Name = "Monitor", IsActive = true });
        return category;
    }

    private UpdateRequisitionCommandHandler BuildHandler(PolicyEvaluationService policyEvaluationService)
    {
        var engine = new ApprovalWorkflowEngine(
            _workflowRepository.Object, _requisitionApprovalRepository.Object, _requisitionRepository.Object, _userRepository.Object);
        return new UpdateRequisitionCommandHandler(
            _requisitionRepository.Object, _categoryRepository.Object, _currentUser.Object, _auditLogger.Object,
            _unitOfWork.Object, engine, policyEvaluationService, _notificationService.Object);
    }

    [Fact]
    public async Task Amend_WithChangedItems_LogsRequisitionAmended_WithAnItemsChangeSummary()
    {
        var category = BuildCategory();
        _categoryRepository.Setup(r => r.GetByIdAsync(_categoryId, It.IsAny<CancellationToken>())).ReturnsAsync(category);

        var requisition = new Requisition
        {
            Id = _requisitionId,
            CompanyId = _companyId,
            CategoryId = _categoryId,
            RequestedByUserId = _userId,
            Status = RequisitionStatus.SentBack,
        };
        requisition.Items.Add(new RequisitionItem { CategoryItemId = _laptopId, ItemName = "Laptop", Quantity = 1 });
        _requisitionRepository.Setup(r => r.GetByIdAsync(_requisitionId, It.IsAny<CancellationToken>())).ReturnsAsync(requisition);

        _currentUser.Setup(c => c.UserId).Returns(_userId);
        _currentUser.Setup(c => c.FullName).Returns("Emma Employee");
        _currentUser.Setup(c => c.Role).Returns(UserRole.Employee);

        var policyEvaluationService = new PolicyEvaluationService(_policyRepository.Object, _userRepository.Object, _requisitionRepository.Object);
        var handler = BuildHandler(policyEvaluationService);

        // Submit=false: amends the SentBack draft without resubmitting yet - isAmendment is driven
        // purely by Status==SentBack, so "RequisitionAmended" is still logged on save.
        var command = new UpdateRequisitionCommand(
            _requisitionId, _categoryId,
            [new RequisitionItemInput(_monitorId, 2)],
            RequisitionPriority.Medium, DateTime.UtcNow.AddDays(7), 0, null, null, null, null,
            [], null, Submit: false);

        await handler.Handle(command, CancellationToken.None);

        _auditLogger.Verify(a => a.LogAsync(
            "RequisitionAmended", nameof(Requisition), _requisitionId,
            It.Is<string>(details => details.Contains("Items:") && details.Contains("Laptop x1") && details.Contains("Monitor x2")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Resubmit_BlockedByIneligibility_LogsEligibilityCheckBlocked_AndPersistsNothing()
    {
        var category = BuildCategory();
        _categoryRepository.Setup(r => r.GetByIdAsync(_categoryId, It.IsAny<CancellationToken>())).ReturnsAsync(category);

        var requisition = new Requisition
        {
            Id = _requisitionId,
            CompanyId = _companyId,
            CategoryId = _categoryId,
            RequestedByUserId = _userId,
            Status = RequisitionStatus.SentBack,
        };
        requisition.Items.Add(new RequisitionItem { CategoryItemId = _laptopId, ItemName = "Laptop", Quantity = 1 });
        _requisitionRepository.Setup(r => r.GetByIdAsync(_requisitionId, It.IsAny<CancellationToken>())).ReturnsAsync(requisition);

        _currentUser.Setup(c => c.UserId).Returns(_userId);
        _currentUser.Setup(c => c.FullName).Returns("Emma Employee");
        _currentUser.Setup(c => c.Role).Returns(UserRole.Employee);

        // A policy this requestor fails (wrong Grade) - same setup as RequisitionEligibilityGateTests.
        var policy = new EligibilityPolicy { CompanyId = _companyId, CategoryId = _categoryId, CategoryItemId = _laptopId, IsActive = true };
        policy.Criteria.Add(new EligibilityPolicyCriterion { CriterionType = EligibilityCriterionType.Grade, AllowedValue = "Manager" });
        _policyRepository
            .Setup(r => r.GetResolvablePolicyAsync(_companyId, _categoryId, _laptopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);
        _userRepository
            .Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = _userId, CompanyId = _companyId, Grade = "Officer" });

        var policyEvaluationService = new PolicyEvaluationService(_policyRepository.Object, _userRepository.Object, _requisitionRepository.Object);
        var handler = BuildHandler(policyEvaluationService);

        var command = new UpdateRequisitionCommand(
            _requisitionId, _categoryId,
            [new RequisitionItemInput(_laptopId, 1)],
            RequisitionPriority.Medium, DateTime.UtcNow.AddDays(7), 0, null, null, null, null,
            [], "Resubmitting.", Submit: true);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();

        _auditLogger.Verify(a => a.LogAsync(
            "EligibilityCheckBlocked", nameof(Requisition), _requisitionId, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        // The gate runs before ReplaceItems/property mutation/SaveChangesAsync - a blocked resubmit
        // must leave the tracked requisition (and the DB) exactly as it was.
        _requisitionRepository.Verify(r => r.ReplaceItems(It.IsAny<Requisition>(), It.IsAny<List<RequisitionItem>>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
