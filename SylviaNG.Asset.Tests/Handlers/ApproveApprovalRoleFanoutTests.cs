using FluentAssertions;
using Moq;
using RMS.Application.Features.ApprovalWorkflows.Services;
using RMS.Application.Features.Approvals.Commands.ApproveApproval;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace SylviaNG.Assets.Tests.Handlers;

/// <summary>
/// End-to-end coverage (through the real ApproveApprovalCommandHandler + real ApprovalWorkflowEngine,
/// with repositories mocked) for the "first responder wins" fix: a Role-type approver fanned out to
/// several users only needs ONE of them to approve. The others' assignments must close out and the
/// stage must complete (advancing the requisition) from that single action alone.
/// </summary>
public class ApproveApprovalRoleFanoutTests
{
    private readonly Mock<IRequisitionApprovalRepository> _requisitionApprovalRepository = new();
    private readonly Mock<IApprovalDelegationRepository> _delegationRepository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IApprovalWorkflowRepository> _workflowRepository = new();
    private readonly Mock<IRequisitionRepository> _requisitionRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();

    [Fact]
    public async Task Approve_FirstOfThreeRoleHolders_CompletesStage_ClosesSiblings_AndAdvancesRequisition()
    {
        var companyId = Guid.NewGuid();
        var requestorId = Guid.NewGuid();
        var deptHead1 = Guid.NewGuid();
        var deptHead2 = Guid.NewGuid();
        var deptHead3 = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var requisition = new Requisition
        {
            CompanyId = companyId,
            CategoryId = Guid.NewGuid(),
            RequestedByUserId = requestorId,
            NeedByDate = DateTime.UtcNow.AddDays(5),
            Status = RequisitionStatus.UnderReview,
        };

        var stage = new ApprovalWorkflowStage { StageOrder = 1, Name = "Department Head Review" };
        stage.Approvers.Add(new WorkflowApprover { ApproverType = ApproverType.Role, ApproverRole = UserRole.DepartmentHead, IsRequired = true });

        var version = new ApprovalWorkflowVersion { Id = Guid.NewGuid(), IsPublished = true, AppliesToAllCategories = true };
        version.Stages.Add(stage);

        var process = new RequisitionApprovalProcess { RequisitionId = requisition.Id, ApprovalWorkflowVersionId = version.Id, Requisition = requisition };

        var approval = new RequisitionApproval
        {
            ApprovalWorkflowStage = stage,
            StageOrder = 1,
            Status = RequisitionApprovalStatus.Pending,
            RequisitionApprovalProcess = process,
        };
        var assignmentDh1 = new RequisitionApprovalAssignment { AssignedUserId = deptHead1, IsRequired = true, RoleFanoutGroupId = groupId };
        var assignmentDh2 = new RequisitionApprovalAssignment { AssignedUserId = deptHead2, IsRequired = true, RoleFanoutGroupId = groupId };
        var assignmentDh3 = new RequisitionApprovalAssignment { AssignedUserId = deptHead3, IsRequired = true, RoleFanoutGroupId = groupId };
        approval.Assignments.Add(assignmentDh1);
        approval.Assignments.Add(assignmentDh2);
        approval.Assignments.Add(assignmentDh3);
        process.StageInstances.Add(approval);

        _requisitionApprovalRepository.Setup(r => r.GetApprovalByIdAsync(approval.Id, It.IsAny<CancellationToken>())).ReturnsAsync(approval);
        _delegationRepository
            .Setup(r => r.GetActiveOnAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApprovalDelegation?)null);
        _workflowRepository.Setup(r => r.GetVersionByIdAsync(version.Id, It.IsAny<CancellationToken>())).ReturnsAsync(version);

        _currentUser.Setup(c => c.UserId).Returns(deptHead2); // the SECOND of the three responds first
        _currentUser.Setup(c => c.FullName).Returns("Dept Head Two");
        _currentUser.Setup(c => c.Role).Returns(UserRole.DepartmentHead);

        var engine = new ApprovalWorkflowEngine(_workflowRepository.Object, _requisitionApprovalRepository.Object, _requisitionRepository.Object, _userRepository.Object);
        var handler = new ApproveApprovalCommandHandler(
            _requisitionApprovalRepository.Object, _delegationRepository.Object, _currentUser.Object, _auditLogger.Object, _unitOfWork.Object, engine);

        await handler.Handle(new ApproveApprovalCommand(approval.Id, "Approved on behalf of the department.", null), CancellationToken.None);

        // The acting assignment (deptHead2) recorded its own action.
        assignmentDh2.HasActed.Should().BeTrue();

        // The two who never acted are closed out - "first responder wins" - so they stop showing as
        // still-pending in their own inboxes/anyone else's view of this approval.
        assignmentDh1.HasActed.Should().BeTrue();
        assignmentDh3.HasActed.Should().BeTrue();

        // One action satisfied the whole (single) role-fanout group, so the stage completed and -
        // since this was the only stage - the requisition advanced straight to Approved.
        approval.Status.Should().Be(RequisitionApprovalStatus.Approved);
        requisition.Status.Should().Be(RequisitionStatus.Approved);

        // Only ONE Approve action was logged for the whole group, not three.
        _requisitionApprovalRepository.Verify(
            r => r.AddAction(It.Is<RequisitionApprovalAction>(a => a.ActionType == ApprovalActionType.Approve)), Times.Once);
    }
}
