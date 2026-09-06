using FluentValidation;
using FluentAssertions;
using Moq;
using RMS.Application.Features.ApprovalWorkflows.Services;
using RMS.Application.Features.Approvals.Commands.PartialApproveApproval;
using RMS.Application.Features.Approvals.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace SylviaNG.Assets.Tests.Handlers;

/// <summary>
/// A partial approval must be a complete split of each item's requested quantity: approved and
/// declined both greater than zero, together adding up to exactly what was requested. This lives in
/// the handler (not PartialApproveApprovalCommandValidator) because it needs each item's real
/// requested quantity, which only exists on the already-loaded Requisition.
/// </summary>
public class PartialApproveApprovalQuantityValidationTests
{
    private readonly Mock<IRequisitionApprovalRepository> _requisitionApprovalRepository = new();
    private readonly Mock<IRequisitionRepository> _requisitionRepository = new();
    private readonly Mock<IApprovalDelegationRepository> _delegationRepository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IApprovalWorkflowRepository> _workflowRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<INotificationService> _notificationService = new();

    private (RequisitionApproval approval, RequisitionItem item, Guid managerId) BuildPendingApproval(int requestedQuantity)
    {
        var managerId = Guid.NewGuid();
        var requisition = new Requisition
        {
            CompanyId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            RequestedByUserId = Guid.NewGuid(),
            NeedByDate = DateTime.UtcNow.AddDays(5),
            Status = RequisitionStatus.UnderReview,
        };
        var item = new RequisitionItem { RequisitionId = requisition.Id, ItemName = "Used Smartphone", Quantity = requestedQuantity };
        requisition.Items.Add(item);

        var stage = new ApprovalWorkflowStage { StageOrder = 1, Name = "Manager Review" };
        var version = new ApprovalWorkflowVersion { Id = Guid.NewGuid(), IsPublished = true, AppliesToAllCategories = true };
        version.Stages.Add(stage); // single-stage workflow - this Manager stage is the only one.
        var process = new RequisitionApprovalProcess
        {
            RequisitionId = requisition.Id, Requisition = requisition, ApprovalWorkflowVersionId = version.Id, CurrentStageOrder = 1,
        };
        var approval = new RequisitionApproval
        {
            ApprovalWorkflowStage = stage,
            StageOrder = 1,
            Status = RequisitionApprovalStatus.Pending,
            RequisitionApprovalProcess = process,
            RequisitionApprovalProcessId = process.Id,
        };
        approval.Assignments.Add(new RequisitionApprovalAssignment { AssignedUserId = managerId, IsRequired = true });
        process.StageInstances.Add(approval);

        _requisitionApprovalRepository.Setup(r => r.GetApprovalByIdAsync(approval.Id, It.IsAny<CancellationToken>())).ReturnsAsync(approval);
        _requisitionApprovalRepository.Setup(r => r.GetProcessByIdAsync(process.Id, It.IsAny<CancellationToken>())).ReturnsAsync(process);
        _workflowRepository.Setup(r => r.GetVersionByIdAsync(version.Id, It.IsAny<CancellationToken>())).ReturnsAsync(version);
        _delegationRepository
            .Setup(r => r.GetActiveOnAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApprovalDelegation?)null);
        // Mimics what a real DbContext's relationship fixup would do: record the action against its
        // owning stage instance, so a later "did this process ever have a PartialApprove action"
        // check (ApprovalWorkflowEngine's terminal-completion branch) can actually see it.
        _requisitionApprovalRepository.Setup(r => r.AddAction(It.IsAny<RequisitionApprovalAction>()))
            .Callback<RequisitionApprovalAction>(a => approval.Actions.Add(a));
        _currentUser.Setup(c => c.UserId).Returns(managerId);
        _currentUser.Setup(c => c.FullName).Returns("Tauhid");
        _currentUser.Setup(c => c.Role).Returns(UserRole.Manager);

        return (approval, item, managerId);
    }

    private PartialApproveApprovalCommandHandler BuildHandler()
    {
        var engine = new ApprovalWorkflowEngine(
            _workflowRepository.Object, _requisitionApprovalRepository.Object, _requisitionRepository.Object, _userRepository.Object);
        return new PartialApproveApprovalCommandHandler(
            _requisitionApprovalRepository.Object, _delegationRepository.Object,
            _currentUser.Object, _auditLogger.Object, _unitOfWork.Object, engine, _notificationService.Object);
    }

    [Fact]
    public async Task PartialApprove_ApprovedOnly_NoDeclined_Throws()
    {
        // The exact reported bug: Requested = 15, Approved = 7, Declined = 0.
        var (approval, item, _) = BuildPendingApproval(requestedQuantity: 15);
        var handler = BuildHandler();
        var command = new PartialApproveApprovalCommand(
            approval.Id, "", [new PartialApprovalDecisionInput(item.Id, 7, 0, null)]);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        approval.Status.Should().Be(RequisitionApprovalStatus.Pending); // nothing was mutated
    }

    [Fact]
    public async Task PartialApprove_ApprovedPlusDeclinedNotEqualToRequested_Throws()
    {
        var (approval, item, _) = BuildPendingApproval(requestedQuantity: 10);
        var handler = BuildHandler();
        var command = new PartialApproveApprovalCommand(
            approval.Id, "", [new PartialApprovalDecisionInput(item.Id, 6, 3, null)]); // 6+3=9 != 10

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task PartialApprove_NegativeDeclined_Throws()
    {
        var (approval, item, _) = BuildPendingApproval(requestedQuantity: 10);
        var handler = BuildHandler();
        var command = new PartialApproveApprovalCommand(
            approval.Id, "", [new PartialApprovalDecisionInput(item.Id, 11, -1, null)]);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task PartialApprove_ValidCompleteSplit_Succeeds()
    {
        // Requested = 15, Approved = 7, Declined = 8 - a complete, valid split. This is a single-stage
        // workflow (Manager only), so once the split is accepted there are no remaining stages and the
        // requisition finalizes immediately - as PartiallyApproved, not Approved, since 8 were declined.
        var (approval, item, _) = BuildPendingApproval(requestedQuantity: 15);
        var handler = BuildHandler();
        var command = new PartialApproveApprovalCommand(
            approval.Id, "", [new PartialApprovalDecisionInput(item.Id, 7, 8, "Limited stock available.")]);

        await handler.Handle(command, CancellationToken.None);

        approval.Status.Should().Be(RequisitionApprovalStatus.Approved);
        approval.RequisitionApprovalProcess!.Requisition!.Status.Should().Be(RequisitionStatus.PartiallyApproved);
        approval.RequisitionApprovalProcess.CompletedAtUtc.Should().NotBeNull();
        _requisitionApprovalRepository.Verify(r => r.AddAction(It.Is<RequisitionApprovalAction>(
            a => a.ActionType == ApprovalActionType.PartialApprove)), Times.Once);
    }

    /// <summary>The actual reported bug: a Manager's Partial Approval on a requisition whose cost
    /// routes it through a second stage (Department Head) used to unconditionally mark the whole
    /// process complete, skipping Department Head entirely. It must instead behave like a normal
    /// Approve - resolve and start the next applicable stage - and only finalize the requisition once
    /// that stage (and any further ones) also completes, at which point the OVERALL outcome must be
    /// PartiallyApproved (some quantity was declined at stage 1), never a plain Approved.</summary>
    [Fact]
    public async Task PartialApprove_WithRemainingStage_AdvancesThenFinalizesAsPartiallyApprovedOnceDeptHeadApproves()
    {
        var managerId = Guid.NewGuid();
        var deptHeadId = Guid.NewGuid();
        var requisition = new Requisition
        {
            CompanyId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            RequestedByUserId = Guid.NewGuid(),
            NeedByDate = DateTime.UtcNow.AddDays(5),
            Status = RequisitionStatus.UnderReview,
            EstimatedCost = 50_000m, // >20,000 and <=100,000 -> Manager -> Department Head, per the real thresholds.
        };
        var item = new RequisitionItem { RequisitionId = requisition.Id, ItemName = "Used Smartphone", Quantity = 10 };
        requisition.Items.Add(item);

        var managerStage = new ApprovalWorkflowStage { StageOrder = 1, Name = "Manager Review" };
        var deptHeadStage = new ApprovalWorkflowStage { StageOrder = 2, Name = "Department Head Review" };
        deptHeadStage.Approvers.Add(new WorkflowApprover { ApproverType = ApproverType.SpecificUser, ApproverUserId = deptHeadId, IsRequired = true });

        var version = new ApprovalWorkflowVersion { Id = Guid.NewGuid(), IsPublished = true, AppliesToAllCategories = true };
        version.Stages.Add(managerStage);
        version.Stages.Add(deptHeadStage);

        var process = new RequisitionApprovalProcess
        {
            RequisitionId = requisition.Id, Requisition = requisition, ApprovalWorkflowVersionId = version.Id, CurrentStageOrder = 1,
        };
        var stage1Approval = new RequisitionApproval
        {
            ApprovalWorkflowStage = managerStage, StageOrder = 1, Status = RequisitionApprovalStatus.Pending,
            RequisitionApprovalProcess = process, RequisitionApprovalProcessId = process.Id,
        };
        stage1Approval.Assignments.Add(new RequisitionApprovalAssignment { AssignedUserId = managerId, IsRequired = true });
        process.StageInstances.Add(stage1Approval);

        _requisitionApprovalRepository.Setup(r => r.GetApprovalByIdAsync(stage1Approval.Id, It.IsAny<CancellationToken>())).ReturnsAsync(stage1Approval);
        _requisitionApprovalRepository.Setup(r => r.GetProcessByIdAsync(process.Id, It.IsAny<CancellationToken>())).ReturnsAsync(process);
        _workflowRepository.Setup(r => r.GetVersionByIdAsync(version.Id, It.IsAny<CancellationToken>())).ReturnsAsync(version);
        _delegationRepository
            .Setup(r => r.GetActiveOnAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApprovalDelegation?)null);
        _userRepository.Setup(r => r.GetByIdAsync(deptHeadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = deptHeadId, FullName = "Emon", IsActive = true, Role = UserRole.DepartmentHead, CompanyId = requisition.CompanyId });
        RequisitionApproval? stage2Approval = null;
        _requisitionApprovalRepository.Setup(r => r.AddApproval(It.IsAny<RequisitionApproval>()))
            .Callback<RequisitionApproval>(a =>
            {
                a.RequisitionApprovalProcess = process;
                process.StageInstances.Add(a);
                if (a.StageOrder == 2) stage2Approval = a;
            });
        _requisitionApprovalRepository.Setup(r => r.AddAssignment(It.IsAny<RequisitionApprovalAssignment>()))
            .Callback<RequisitionApprovalAssignment>(assignment => stage2Approval!.Assignments.Add(assignment));
        // Mimics what a real DbContext's relationship fixup would do: record each action against its
        // owning stage instance, so the terminal-completion "did this process ever have a
        // PartialApprove action" check can actually see stage 1's.
        _requisitionApprovalRepository.Setup(r => r.AddAction(It.IsAny<RequisitionApprovalAction>()))
            .Callback<RequisitionApprovalAction>(a => process.StageInstances.First(s => s.Id == a.RequisitionApprovalId).Actions.Add(a));

        _currentUser.Setup(c => c.UserId).Returns(managerId);
        _currentUser.Setup(c => c.FullName).Returns("Tauhid");
        _currentUser.Setup(c => c.Role).Returns(UserRole.Manager);

        var handler = BuildHandler();
        var command = new PartialApproveApprovalCommand(
            stage1Approval.Id, "", [new PartialApprovalDecisionInput(item.Id, 6, 4, null)]);

        await handler.Handle(command, CancellationToken.None);

        // Stage 1 itself is done, but Department Head must now be the one pending - NOT the whole
        // requisition finalized.
        stage1Approval.Status.Should().Be(RequisitionApprovalStatus.Approved);
        requisition.Status.Should().Be(RequisitionStatus.UnderReview);
        process.CurrentStageOrder.Should().Be(2);
        process.CompletedAtUtc.Should().BeNull();
        stage2Approval.Should().NotBeNull();
        stage2Approval!.Status.Should().Be(RequisitionApprovalStatus.Pending);
        _requisitionApprovalRepository.Setup(r => r.GetApprovalByIdAsync(stage2Approval.Id, It.IsAny<CancellationToken>())).ReturnsAsync(stage2Approval);

        // Now Department Head approves the (already-decided) 6/4 split - this must be what finally
        // resolves the requisition, and it must resolve as PartiallyApproved, not Approved, because a
        // decline happened at stage 1.
        var engine = new ApprovalWorkflowEngine(
            _workflowRepository.Object, _requisitionApprovalRepository.Object, _requisitionRepository.Object, _userRepository.Object);
        var approveHandler = new RMS.Application.Features.Approvals.Commands.ApproveApproval.ApproveApprovalCommandHandler(
            _requisitionApprovalRepository.Object, _delegationRepository.Object, _currentUser.Object, _auditLogger.Object,
            _unitOfWork.Object, engine, _notificationService.Object);
        _currentUser.Setup(c => c.UserId).Returns(deptHeadId);
        _currentUser.Setup(c => c.FullName).Returns("Emon");
        _currentUser.Setup(c => c.Role).Returns(UserRole.DepartmentHead);

        await approveHandler.Handle(
            new RMS.Application.Features.Approvals.Commands.ApproveApproval.ApproveApprovalCommand(
                stage2Approval.Id, "Confirmed with the Manager's split."),
            CancellationToken.None);

        requisition.Status.Should().Be(RequisitionStatus.PartiallyApproved);
        process.CompletedAtUtc.Should().NotBeNull();
    }
}
