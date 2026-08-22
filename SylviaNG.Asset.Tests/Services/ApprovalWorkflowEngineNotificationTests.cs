using FluentAssertions;
using Moq;
using RMS.Application.Features.ApprovalWorkflows.Services;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace SylviaNG.Assets.Tests.Services;

/// <summary>
/// Feature 9 (US-029): ApprovalWorkflowEngine.StartStageAsync is the single hook point for "a
/// requisition just entered someone's approval queue" - these tests confirm it returns one
/// ApprovalQueueEntry NotificationRequest per assignee, for both a Role-fanout stage (several
/// recipients) and a SpecificUser stage (one recipient), and that reaching full approval with no
/// further stage returns a RequisitionApproved notification to the requestor instead. The engine
/// itself never sends anything (NotificationService saves eagerly - see its own remarks) - it only
/// returns what the caller should send after its own SaveChangesAsync succeeds.
/// </summary>
public class ApprovalWorkflowEngineNotificationTests
{
    private readonly Mock<IApprovalWorkflowRepository> _workflowRepository = new();
    private readonly Mock<IRequisitionApprovalRepository> _requisitionApprovalRepository = new();
    private readonly Mock<IRequisitionRepository> _requisitionRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _categoryId = Guid.NewGuid();
    private readonly Guid _requestorId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();

    private ApprovalWorkflowEngine BuildEngine() => new(
        _workflowRepository.Object, _requisitionApprovalRepository.Object, _requisitionRepository.Object, _userRepository.Object);

    private Requisition BuildRequisition()
    {
        var requisition = new Requisition
        {
            CompanyId = _companyId,
            CategoryId = _categoryId,
            RequestedByUserId = _requestorId,
            RequisitionNumber = "REQ-2026-00099",
            Priority = RequisitionPriority.Medium,
            Status = RequisitionStatus.Submitted,
        };
        requisition.Items.Add(new RequisitionItem { ItemName = "Laptop", Quantity = 2 });
        return requisition;
    }

    [Fact]
    public async Task ResolveAndStartAsync_RoleFanoutStage_ReturnsOneApprovalQueueEntryPerAssignee()
    {
        var deptHead1 = Guid.NewGuid();
        var deptHead2 = Guid.NewGuid();

        var stage = new ApprovalWorkflowStage { StageOrder = 1, Name = "Department Head Review" };
        stage.Approvers.Add(new WorkflowApprover { ApproverType = ApproverType.Role, ApproverRole = UserRole.DepartmentHead, IsRequired = true });
        var version = new ApprovalWorkflowVersion { Id = Guid.NewGuid(), IsPublished = true, AppliesToAllCategories = true };
        version.Stages.Add(stage);

        _workflowRepository.Setup(r => r.GetResolvableVersionAsync(_companyId, _categoryId, It.IsAny<CancellationToken>())).ReturnsAsync(version);
        _userRepository.Setup(r => r.GetActiveByRoleAsync(_companyId, UserRole.DepartmentHead, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new User { Id = deptHead1, CompanyId = _companyId, IsActive = true },
                new User { Id = deptHead2, CompanyId = _companyId, IsActive = true },
            ]);
        _userRepository.Setup(r => r.GetByIdAsync(_requestorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = _requestorId, FullName = "Emma Employee", Department = "Engineering" });

        var requisition = BuildRequisition();
        var engine = BuildEngine();

        var notifications = await engine.ResolveAndStartAsync(requisition, _actorId, "Emma Employee", "Employee", CancellationToken.None);

        notifications.Should().HaveCount(2);
        notifications.Should().OnlyContain(n => n.EventType == NotificationEventType.ApprovalQueueEntry);
        notifications.Select(n => n.RecipientUserId).Should().BeEquivalentTo([deptHead1, deptHead2]);
        notifications.Should().OnlyContain(n => n.RequisitionId == requisition.Id && n.CompanyId == _companyId);
        notifications[0].MergeTags["RequisitionNumber"].Should().Be("REQ-2026-00099");
        notifications[0].MergeTags["Requestor"].Should().Be("Emma Employee");
    }

    [Fact]
    public async Task ResolveAndStartAsync_SpecificUserStage_ReturnsOneApprovalQueueEntry()
    {
        var approverId = Guid.NewGuid();

        var stage = new ApprovalWorkflowStage { StageOrder = 1, Name = "Manager Review" };
        stage.Approvers.Add(new WorkflowApprover { ApproverType = ApproverType.SpecificUser, ApproverUserId = approverId, IsRequired = true });
        var version = new ApprovalWorkflowVersion { Id = Guid.NewGuid(), IsPublished = true, AppliesToAllCategories = true };
        version.Stages.Add(stage);

        _workflowRepository.Setup(r => r.GetResolvableVersionAsync(_companyId, _categoryId, It.IsAny<CancellationToken>())).ReturnsAsync(version);
        _userRepository.Setup(r => r.GetByIdAsync(approverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = approverId, CompanyId = _companyId, IsActive = true });
        _userRepository.Setup(r => r.GetByIdAsync(_requestorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = _requestorId, FullName = "Emma Employee" });

        var requisition = BuildRequisition();
        var engine = BuildEngine();

        var notifications = await engine.ResolveAndStartAsync(requisition, _actorId, "Emma Employee", "Employee", CancellationToken.None);

        notifications.Should().ContainSingle();
        notifications[0].EventType.Should().Be(NotificationEventType.ApprovalQueueEntry);
        notifications[0].RecipientUserId.Should().Be(approverId);
    }

    [Fact]
    public async Task ResolveAndStartAsync_EveryApproverIsTheRequestor_AutoSkipsAndReturnsRequisitionApprovedToRequestor()
    {
        var stage = new ApprovalWorkflowStage { StageOrder = 1, Name = "Self Review" };
        stage.Approvers.Add(new WorkflowApprover { ApproverType = ApproverType.SpecificUser, ApproverUserId = _requestorId, IsRequired = true });
        var version = new ApprovalWorkflowVersion { Id = Guid.NewGuid(), IsPublished = true, AppliesToAllCategories = true };
        version.Stages.Add(stage);

        _workflowRepository.Setup(r => r.GetResolvableVersionAsync(_companyId, _categoryId, It.IsAny<CancellationToken>())).ReturnsAsync(version);
        _userRepository.Setup(r => r.GetByIdAsync(_requestorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = _requestorId, CompanyId = _companyId, IsActive = true, FullName = "Emma Employee" });

        var requisition = BuildRequisition();
        var engine = BuildEngine();

        var notifications = await engine.ResolveAndStartAsync(requisition, _actorId, "System", null, CancellationToken.None);

        notifications.Should().ContainSingle();
        notifications[0].EventType.Should().Be(NotificationEventType.RequisitionApproved);
        notifications[0].RecipientUserId.Should().Be(_requestorId);
    }
}
