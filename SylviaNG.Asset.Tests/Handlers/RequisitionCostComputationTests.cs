using FluentAssertions;
using Moq;
using RMS.Application.Features.ApprovalWorkflows.Services;
using RMS.Application.Features.EligibilityPolicies.Services;
using RMS.Application.Features.Requisitions;
using RMS.Application.Features.Requisitions.Commands.CreateRequisition;
using RMS.Application.Features.Requisitions.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace SylviaNG.Assets.Tests.Handlers;

/// <summary>
/// Cost is never typed by the requestor or an approver - it's always computed from the Admin's own
/// catalog price (RequisitionCategory -&gt; CategoryItem.Price) x the quantity actually requested, the
/// moment a requisition is created. See RequisitionFieldValidation.ComputeEstimatedCost.
/// </summary>
public class RequisitionCostComputationTests
{
    private readonly Mock<IRequisitionRepository> _requisitionRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IApprovalWorkflowRepository> _workflowRepository = new();
    private readonly Mock<IRequisitionApprovalRepository> _requisitionApprovalRepository = new();
    private readonly Mock<IEligibilityPolicyRepository> _policyRepository = new();
    private readonly Mock<INotificationService> _notificationService = new();

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _categoryId = Guid.NewGuid();
    private readonly Guid _laptopId = Guid.NewGuid();
    private readonly Guid _monitorId = Guid.NewGuid();
    private readonly Guid _unpricedId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private readonly CreateRequisitionCommandHandler _handler;

    public RequisitionCostComputationTests()
    {
        _currentUser.Setup(c => c.CompanyId).Returns(_companyId);
        _currentUser.Setup(c => c.UserId).Returns(_userId);
        _currentUser.Setup(c => c.FullName).Returns("Emma Employee");
        _currentUser.Setup(c => c.Role).Returns(UserRole.Employee);

        var category = new RequisitionCategory { Id = _categoryId, CompanyId = _companyId, IsActive = true, CurrentVersionNumber = 1 };
        category.Items.Add(new CategoryItem { Id = _laptopId, Name = "Laptop", IsActive = true, Price = 600000m });
        category.Items.Add(new CategoryItem { Id = _monitorId, Name = "Monitor", IsActive = true, Price = 20000m });
        category.Items.Add(new CategoryItem { Id = _unpricedId, Name = "Cable", IsActive = true, Price = null });
        _categoryRepository.Setup(r => r.GetByIdAsync(_categoryId, It.IsAny<CancellationToken>())).ReturnsAsync(category);

        var policyEvaluationService = new PolicyEvaluationService(_policyRepository.Object, _userRepository.Object, _requisitionRepository.Object);
        var approvalWorkflowEngine = new ApprovalWorkflowEngine(
            _workflowRepository.Object, _requisitionApprovalRepository.Object, _requisitionRepository.Object, _userRepository.Object);

        _handler = new CreateRequisitionCommandHandler(
            _requisitionRepository.Object, _categoryRepository.Object, _currentUser.Object, _auditLogger.Object, _unitOfWork.Object,
            approvalWorkflowEngine, policyEvaluationService, _notificationService.Object);

        _requisitionRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Requisition?)null);
    }

    [Fact]
    public async Task Create_SumsQuantityTimesCatalogPrice_AcrossMultipleItems()
    {
        Requisition? captured = null;
        _requisitionRepository.Setup(r => r.Add(It.IsAny<Requisition>())).Callback<Requisition>(r => captured = r);

        var command = new CreateRequisitionCommand(
            _categoryId,
            [new RequisitionItemInput(_laptopId, 2), new RequisitionItemInput(_monitorId, 3)],
            RequisitionPriority.Medium, DateTime.UtcNow.AddDays(7), null, null, null, null, [], Submit: false);

        await _handler.Handle(command, CancellationToken.None);

        // 2 x 600,000 (Laptop) + 3 x 20,000 (Monitor) = 1,260,000 - never a number the client supplied.
        captured.Should().NotBeNull();
        captured!.EstimatedCost.Should().Be(1260000m);
    }

    [Fact]
    public async Task Create_ItemWithNoAdminSetPrice_ContributesZero_NotAnError()
    {
        Requisition? captured = null;
        _requisitionRepository.Setup(r => r.Add(It.IsAny<Requisition>())).Callback<Requisition>(r => captured = r);

        var command = new CreateRequisitionCommand(
            _categoryId,
            [new RequisitionItemInput(_unpricedId, 5)],
            RequisitionPriority.Medium, DateTime.UtcNow.AddDays(7), null, null, null, null, [], Submit: false);

        await _handler.Handle(command, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.EstimatedCost.Should().Be(0m);
    }
}
