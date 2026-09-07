using FluentAssertions;
using Moq;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.ApprovalWorkflows.Services;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace SylviaNG.Assets.Tests.Services;

public class ApprovalWorkflowEngineTests
{
    private readonly Mock<IApprovalWorkflowRepository> _workflowRepository = new();
    private readonly Mock<IRequisitionApprovalRepository> _requisitionApprovalRepository = new();
    private readonly Mock<IRequisitionRepository> _requisitionRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly ApprovalWorkflowEngine _engine;

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _categoryId = Guid.NewGuid();
    private readonly Guid _requestorId = Guid.NewGuid();

    public ApprovalWorkflowEngineTests()
    {
        _engine = new ApprovalWorkflowEngine(
            _workflowRepository.Object, _requisitionApprovalRepository.Object, _requisitionRepository.Object, _userRepository.Object);

        // Every AddXxx call just needs to succeed - the engine's own returned/mutated objects are what
        // tests assert against, not persistence itself (that's Infrastructure's job, exercised by the
        // real migration/DB verification, not these unit tests).
        _requisitionApprovalRepository.Setup(r => r.AddProcess(It.IsAny<RequisitionApprovalProcess>()));
        _requisitionApprovalRepository.Setup(r => r.AddApproval(It.IsAny<RequisitionApproval>()));
        _requisitionApprovalRepository.Setup(r => r.AddAssignment(It.IsAny<RequisitionApprovalAssignment>()));
        _requisitionApprovalRepository.Setup(r => r.AddAction(It.IsAny<RequisitionApprovalAction>()));
        _requisitionRepository.Setup(r => r.AddStatusHistory(It.IsAny<RequisitionStatusHistory>()));
    }

    private Requisition NewRequisition(decimal estimatedCost = 0) => new()
    {
        CompanyId = _companyId,
        CategoryId = _categoryId,
        RequestedByUserId = _requestorId,
        NeedByDate = DateTime.UtcNow.AddDays(7),
        EstimatedCost = estimatedCost,
        Status = RequisitionStatus.Submitted,
    };

    private static ApprovalWorkflowVersion NewVersion(params ApprovalWorkflowStage[] stages)
    {
        var version = new ApprovalWorkflowVersion { IsPublished = true, AppliesToAllCategories = true };
        foreach (var stage in stages)
        {
            version.Stages.Add(stage);
        }
        return version;
    }

    private static ApprovalWorkflowStage NewStage(int order, bool capturesCost = false) => new()
    {
        StageOrder = order,
        Name = $"Stage {order}",
        CapturesEstimatedCost = capturesCost,
    };

    [Fact]
    public async Task ResolveAndStartAsync_NoConfiguredWorkflow_ThrowsConflict()
    {
        _workflowRepository
            .Setup(r => r.GetResolvableVersionAsync(_companyId, _categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApprovalWorkflowVersion?)null);

        var requisition = NewRequisition();

        var act = () => _engine.ResolveAndStartAsync(requisition, _requestorId, "Employee One", "Employee", CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task ResolveAndStartAsync_ZeroStageWorkflow_ThrowsConflict()
    {
        var version = NewVersion(); // no stages
        _workflowRepository
            .Setup(r => r.GetResolvableVersionAsync(_companyId, _categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);

        var requisition = NewRequisition();

        var act = () => _engine.ResolveAndStartAsync(requisition, _requestorId, "Employee One", "Employee", CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task ResolveAndStartAsync_SingleStageWithApprover_TransitionsToUnderReview_AndAssignsApprover()
    {
        var approverId = Guid.NewGuid();
        var stage = NewStage(1);
        stage.Approvers.Add(new WorkflowApprover { ApproverType = ApproverType.SpecificUser, ApproverUserId = approverId, IsRequired = true });
        var version = NewVersion(stage);

        _workflowRepository
            .Setup(r => r.GetResolvableVersionAsync(_companyId, _categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);
        _userRepository
            .Setup(r => r.GetByIdAsync(approverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = approverId, FullName = "Approver One", IsActive = true, CompanyId = _companyId });

        RequisitionApprovalAssignment? capturedAssignment = null;
        _requisitionApprovalRepository
            .Setup(r => r.AddAssignment(It.IsAny<RequisitionApprovalAssignment>()))
            .Callback<RequisitionApprovalAssignment>(a => capturedAssignment = a);

        var requisition = NewRequisition();

        await _engine.ResolveAndStartAsync(requisition, _requestorId, "Employee One", "Employee", CancellationToken.None);

        requisition.Status.Should().Be(RequisitionStatus.UnderReview);
        capturedAssignment.Should().NotBeNull();
        capturedAssignment!.AssignedUserId.Should().Be(approverId);
        capturedAssignment.IsRequired.Should().BeTrue();
        capturedAssignment.HasActed.Should().BeFalse();
        _requisitionApprovalRepository.Verify(r => r.AddApproval(It.Is<RequisitionApproval>(a => a.Status == RequisitionApprovalStatus.Pending)), Times.Once);
    }

    [Fact]
    public async Task ResolveAndStartAsync_SoleApproverIsRequestor_AutoSkipsStage_AndApprovesRequisitionImmediately()
    {
        // Self-approval rule: a stage whose resolved approver(s) all equal the requestor is auto-skipped.
        var stage = NewStage(1);
        stage.Approvers.Add(new WorkflowApprover { ApproverType = ApproverType.SpecificUser, ApproverUserId = _requestorId, IsRequired = true });
        var version = NewVersion(stage);

        _workflowRepository
            .Setup(r => r.GetResolvableVersionAsync(_companyId, _categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);
        _userRepository
            .Setup(r => r.GetByIdAsync(_requestorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = _requestorId, FullName = "Self Approver", IsActive = true, CompanyId = _companyId });

        var requisition = NewRequisition();

        await _engine.ResolveAndStartAsync(requisition, _requestorId, "Employee One", "Employee", CancellationToken.None);

        requisition.Status.Should().Be(RequisitionStatus.Approved);
        _requisitionApprovalRepository.Verify(
            r => r.AddAction(It.Is<RequisitionApprovalAction>(a => a.ActionType == ApprovalActionType.AutoSkip)), Times.Once);
        _requisitionApprovalRepository.Verify(r => r.AddAssignment(It.IsAny<RequisitionApprovalAssignment>()), Times.Never);
    }

    [Fact]
    public async Task ResolveAndStartAsync_SelfSkippedCapturingStage_StillEvaluatesCostConditionalNextStage()
    {
        // Requisition.EstimatedCost is always a real, deterministic value by resolution time - computed
        // from the Admin's catalog prices when the requisition was created, independent of any stage's
        // approval status. So a Line Manager submitting their own request (sole approver on Stage 1,
        // auto-skipped) must NOT change whether Stage 2's Cost condition applies - it's evaluated against
        // the real pre-known cost either way, keeping a human in the loop when the cost warrants it.
        var deptHead = Guid.NewGuid();
        var stage1 = NewStage(1, capturesCost: true);
        stage1.Approvers.Add(new WorkflowApprover { ApproverType = ApproverType.SpecificUser, ApproverUserId = _requestorId, IsRequired = true });
        var stage2 = NewStage(2);
        stage2.Approvers.Add(new WorkflowApprover { ApproverType = ApproverType.SpecificUser, ApproverUserId = deptHead, IsRequired = true });
        stage2.Conditions.Add(new ApprovalWorkflowStageCondition { ConditionType = ApprovalConditionType.Cost, MinCost = 20000m });
        var version = NewVersion(stage1, stage2);

        _workflowRepository
            .Setup(r => r.GetResolvableVersionAsync(_companyId, _categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);
        _userRepository.Setup(r => r.GetByIdAsync(_requestorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = _requestorId, FullName = "Self Approver", IsActive = true, CompanyId = _companyId });
        _userRepository.Setup(r => r.GetByIdAsync(deptHead, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = deptHead, FullName = "Dept Head", IsActive = true, CompanyId = _companyId });

        var requisition = NewRequisition(estimatedCost: 25000m); // computed at creation, above Stage 2's threshold

        await _engine.ResolveAndStartAsync(requisition, _requestorId, "Employee One", "Employee", CancellationToken.None);

        requisition.Status.Should().Be(RequisitionStatus.UnderReview); // NOT auto-approved
        _requisitionApprovalRepository.Verify(
            r => r.AddAction(It.Is<RequisitionApprovalAction>(a => a.ActionType == ApprovalActionType.AutoSkip)), Times.Once);
        _requisitionApprovalRepository.Verify(
            r => r.AddAssignment(It.Is<RequisitionApprovalAssignment>(a => a.AssignedUserId == deptHead)), Times.Once);
    }

    [Fact]
    public async Task ResolveAndStartAsync_TwoSequentialStages_OnlyStartsFirstStage()
    {
        var approver1 = Guid.NewGuid();
        var approver2 = Guid.NewGuid();
        var stage1 = NewStage(1);
        stage1.Approvers.Add(new WorkflowApprover { ApproverType = ApproverType.SpecificUser, ApproverUserId = approver1, IsRequired = true });
        var stage2 = NewStage(2);
        stage2.Approvers.Add(new WorkflowApprover { ApproverType = ApproverType.SpecificUser, ApproverUserId = approver2, IsRequired = true });
        var version = NewVersion(stage1, stage2);

        _workflowRepository
            .Setup(r => r.GetResolvableVersionAsync(_companyId, _categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);
        _userRepository.Setup(r => r.GetByIdAsync(approver1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = approver1, FullName = "A1", IsActive = true, CompanyId = _companyId });
        _userRepository.Setup(r => r.GetByIdAsync(approver2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = approver2, FullName = "A2", IsActive = true, CompanyId = _companyId });

        var requisition = NewRequisition();

        await _engine.ResolveAndStartAsync(requisition, _requestorId, "Employee One", "Employee", CancellationToken.None);

        // Only ONE RequisitionApproval materialized at resolution time - stage 2 is resolved lazily
        // once stage 1 completes (AdvanceAfterApprovalAsync), not upfront.
        _requisitionApprovalRepository.Verify(r => r.AddApproval(It.IsAny<RequisitionApproval>()), Times.Once);
        _userRepository.Verify(r => r.GetByIdAsync(approver2, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveAndStartAsync_ParallelStageWithTwoRequiredApprovers_CreatesTwoAssignments()
    {
        var approver1 = Guid.NewGuid();
        var approver2 = Guid.NewGuid();
        var stage = NewStage(1);
        stage.Approvers.Add(new WorkflowApprover { ApproverType = ApproverType.SpecificUser, ApproverUserId = approver1, IsRequired = true });
        stage.Approvers.Add(new WorkflowApprover { ApproverType = ApproverType.SpecificUser, ApproverUserId = approver2, IsRequired = true });
        var version = NewVersion(stage);

        _workflowRepository
            .Setup(r => r.GetResolvableVersionAsync(_companyId, _categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);
        _userRepository.Setup(r => r.GetByIdAsync(approver1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = approver1, FullName = "A1", IsActive = true, CompanyId = _companyId });
        _userRepository.Setup(r => r.GetByIdAsync(approver2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = approver2, FullName = "A2", IsActive = true, CompanyId = _companyId });

        var requisition = NewRequisition();

        await _engine.ResolveAndStartAsync(requisition, _requestorId, "Employee One", "Employee", CancellationToken.None);

        _requisitionApprovalRepository.Verify(r => r.AddAssignment(It.IsAny<RequisitionApprovalAssignment>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ResolveAndStartAsync_RoleApproverWithMultipleActiveUsers_FansOutToAllSharingOneGroupId()
    {
        // Fix: a Role-type approver with several active users in that role is "any one of them", not
        // "all of them" - all still get an assignment (so all see it in their inbox), sharing one
        // RoleFanoutGroupId that marks them as one logical slot.
        var deptHead1 = Guid.NewGuid();
        var deptHead2 = Guid.NewGuid();
        var deptHead3 = Guid.NewGuid();
        var stage = NewStage(1);
        stage.Approvers.Add(new WorkflowApprover { ApproverType = ApproverType.Role, ApproverRole = UserRole.DepartmentHead, IsRequired = true });
        var version = NewVersion(stage);

        _workflowRepository
            .Setup(r => r.GetResolvableVersionAsync(_companyId, _categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);
        _userRepository
            .Setup(r => r.GetActiveByRoleAsync(_companyId, UserRole.DepartmentHead, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new User { Id = deptHead1, FullName = "DH1", IsActive = true, CompanyId = _companyId, Role = UserRole.DepartmentHead },
                new User { Id = deptHead2, FullName = "DH2", IsActive = true, CompanyId = _companyId, Role = UserRole.DepartmentHead },
                new User { Id = deptHead3, FullName = "DH3", IsActive = true, CompanyId = _companyId, Role = UserRole.DepartmentHead },
            ]);

        var capturedAssignments = new List<RequisitionApprovalAssignment>();
        _requisitionApprovalRepository
            .Setup(r => r.AddAssignment(It.IsAny<RequisitionApprovalAssignment>()))
            .Callback<RequisitionApprovalAssignment>(a => capturedAssignments.Add(a));

        var requisition = NewRequisition();

        await _engine.ResolveAndStartAsync(requisition, _requestorId, "Employee One", "Employee", CancellationToken.None);

        capturedAssignments.Should().HaveCount(3);
        capturedAssignments.Select(a => a.AssignedUserId).Should().BeEquivalentTo([deptHead1, deptHead2, deptHead3]);
        capturedAssignments.Should().OnlyContain(a => a.IsRequired);
        capturedAssignments.Select(a => a.RoleFanoutGroupId).Distinct().Should().ContainSingle();
        capturedAssignments.Should().OnlyContain(a => a.RoleFanoutGroupId != null);
    }

    [Fact]
    public async Task ResolveAndStartAsync_RoleApproverWithExactlyOneActiveUser_NoGrouping()
    {
        var soleDeptHead = Guid.NewGuid();
        var stage = NewStage(1);
        stage.Approvers.Add(new WorkflowApprover { ApproverType = ApproverType.Role, ApproverRole = UserRole.DepartmentHead, IsRequired = true });
        var version = NewVersion(stage);

        _workflowRepository
            .Setup(r => r.GetResolvableVersionAsync(_companyId, _categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);
        _userRepository
            .Setup(r => r.GetActiveByRoleAsync(_companyId, UserRole.DepartmentHead, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new User { Id = soleDeptHead, FullName = "DH1", IsActive = true, CompanyId = _companyId, Role = UserRole.DepartmentHead }]);

        RequisitionApprovalAssignment? captured = null;
        _requisitionApprovalRepository
            .Setup(r => r.AddAssignment(It.IsAny<RequisitionApprovalAssignment>()))
            .Callback<RequisitionApprovalAssignment>(a => captured = a);

        var requisition = NewRequisition();

        await _engine.ResolveAndStartAsync(requisition, _requestorId, "Employee One", "Employee", CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.RoleFanoutGroupId.Should().BeNull();
    }

    [Fact]
    public void IsStageComplete_RoleFanoutGroup_OneMemberActed_ReturnsTrue_FirstResponderWins()
    {
        var groupId = Guid.NewGuid();
        var approval = new RequisitionApproval();
        approval.Assignments.Add(new RequisitionApprovalAssignment { IsRequired = true, RoleFanoutGroupId = groupId, HasActed = true });
        approval.Assignments.Add(new RequisitionApprovalAssignment { IsRequired = true, RoleFanoutGroupId = groupId, HasActed = false });
        approval.Assignments.Add(new RequisitionApprovalAssignment { IsRequired = true, RoleFanoutGroupId = groupId, HasActed = false });

        ApprovalWorkflowEngine.IsStageComplete(approval).Should().BeTrue();
    }

    [Fact]
    public void IsStageComplete_RoleFanoutGroupUnacted_PlusSeparateRequiredApprover_ReturnsFalseUntilBothSatisfied()
    {
        // A true-parallel separate WorkflowApprover row (e.g. Finance, own required slot) is unaffected
        // by a role-fanout group elsewhere on the same stage - both must be satisfied independently.
        var groupId = Guid.NewGuid();
        var approval = new RequisitionApproval();
        approval.Assignments.Add(new RequisitionApprovalAssignment { IsRequired = true, RoleFanoutGroupId = groupId, HasActed = false });
        approval.Assignments.Add(new RequisitionApprovalAssignment { IsRequired = true, RoleFanoutGroupId = groupId, HasActed = false });
        approval.Assignments.Add(new RequisitionApprovalAssignment { IsRequired = true, RoleFanoutGroupId = null, HasActed = true }); // Finance, already acted

        ApprovalWorkflowEngine.IsStageComplete(approval).Should().BeFalse();

        approval.Assignments[0].HasActed = true; // one of the role-fanout group now acts

        ApprovalWorkflowEngine.IsStageComplete(approval).Should().BeTrue();
    }

    [Fact]
    public void CloseRoleFanoutSiblings_OneGroupMemberActs_ClosesOtherUnactedGroupMembers_LeavesUnrelatedAssignmentsAlone()
    {
        var groupId = Guid.NewGuid();
        var approval = new RequisitionApproval();
        var acted = new RequisitionApprovalAssignment { IsRequired = true, RoleFanoutGroupId = groupId, HasActed = true, ActedAtUtc = DateTime.UtcNow };
        var sibling1 = new RequisitionApprovalAssignment { IsRequired = true, RoleFanoutGroupId = groupId, HasActed = false };
        var sibling2 = new RequisitionApprovalAssignment { IsRequired = true, RoleFanoutGroupId = groupId, HasActed = false };
        var unrelated = new RequisitionApprovalAssignment { IsRequired = true, RoleFanoutGroupId = null, HasActed = false }; // separate parallel approver
        approval.Assignments.AddRange([acted, sibling1, sibling2, unrelated]);

        ApprovalWorkflowEngine.CloseRoleFanoutSiblings(approval, acted);

        sibling1.HasActed.Should().BeTrue();
        sibling1.ActedAtUtc.Should().NotBeNull();
        sibling2.HasActed.Should().BeTrue();
        unrelated.HasActed.Should().BeFalse();
    }

    [Fact]
    public void CloseRoleFanoutSiblings_UngroupedAssignment_IsNoOp()
    {
        var approval = new RequisitionApproval();
        var acted = new RequisitionApprovalAssignment { IsRequired = true, RoleFanoutGroupId = null, HasActed = true };
        var other = new RequisitionApprovalAssignment { IsRequired = true, RoleFanoutGroupId = null, HasActed = false };
        approval.Assignments.AddRange([acted, other]);

        ApprovalWorkflowEngine.CloseRoleFanoutSiblings(approval, acted);

        other.HasActed.Should().BeFalse();
    }

    [Fact]
    public void IsStageComplete_AllRequiredActed_ReturnsTrue_IgnoringOptionalUnacted()
    {
        var approval = new RequisitionApproval();
        approval.Assignments.Add(new RequisitionApprovalAssignment { IsRequired = true, HasActed = true });
        approval.Assignments.Add(new RequisitionApprovalAssignment { IsRequired = false, HasActed = false });

        ApprovalWorkflowEngine.IsStageComplete(approval).Should().BeTrue();
    }

    [Fact]
    public void IsStageComplete_OneRequiredUnacted_ReturnsFalse()
    {
        var approval = new RequisitionApproval();
        approval.Assignments.Add(new RequisitionApprovalAssignment { IsRequired = true, HasActed = true });
        approval.Assignments.Add(new RequisitionApprovalAssignment { IsRequired = true, HasActed = false });

        ApprovalWorkflowEngine.IsStageComplete(approval).Should().BeFalse();
    }

    [Fact]
    public async Task AdvanceAfterApprovalAsync_NoRemainingStages_ApprovesRequisition()
    {
        var stage = NewStage(1);
        var version = new ApprovalWorkflowVersion { Id = Guid.NewGuid(), IsPublished = true, AppliesToAllCategories = true };
        version.Stages.Add(stage);

        var requisition = NewRequisition();
        requisition.Status = RequisitionStatus.UnderReview; // ResolveAndStart would already have set this before any stage could complete
        var process = new RequisitionApprovalProcess { RequisitionId = requisition.Id, ApprovalWorkflowVersionId = version.Id, Requisition = requisition };
        var completedApproval = new RequisitionApproval
        {
            ApprovalWorkflowStage = stage,
            StageOrder = 1,
            Status = RequisitionApprovalStatus.Approved,
            RequisitionApprovalProcess = process,
        };

        _workflowRepository.Setup(r => r.GetVersionByIdAsync(version.Id, It.IsAny<CancellationToken>())).ReturnsAsync(version);

        await _engine.AdvanceAfterApprovalAsync(completedApproval, _requestorId, "Approver", "Manager", CancellationToken.None);

        requisition.Status.Should().Be(RequisitionStatus.Approved);
        process.CompletedAtUtc.Should().NotBeNull();
        process.CurrentStageOrder.Should().BeNull();
    }

    [Fact]
    public async Task AdvanceAfterApprovalAsync_CostAlreadyOnRequisitionFromCreation_EvaluatesCostConditionOnNextStage()
    {
        // Feature 3 correction: cost is computed once, from the Admin's catalog price x quantity, when
        // the requisition is created (RequisitionFieldValidation.ComputeEstimatedCost) - it's never
        // typed by an approver during Approve, so it's already sitting on the requisition by the time
        // any stage completes. This is that steady-state case: stage 1 finishes, stage 2's Cost
        // condition reads the value that was already there.
        var stage1 = NewStage(1, capturesCost: true);
        var conditionalApprover = Guid.NewGuid();
        var stage2 = NewStage(2);
        stage2.Conditions.Add(new ApprovalWorkflowStageCondition { ConditionType = ApprovalConditionType.Cost, MinCost = 20000m });
        stage2.Approvers.Add(new WorkflowApprover { ApproverType = ApproverType.SpecificUser, ApproverUserId = conditionalApprover, IsRequired = true });

        var version = new ApprovalWorkflowVersion { Id = Guid.NewGuid(), IsPublished = true, AppliesToAllCategories = true };
        version.Stages.Add(stage1);
        version.Stages.Add(stage2);

        var requisition = NewRequisition(estimatedCost: 25000m);
        requisition.Status = RequisitionStatus.UnderReview; // ResolveAndStart would already have set this before stage 1 could complete
        var process = new RequisitionApprovalProcess { RequisitionId = requisition.Id, ApprovalWorkflowVersionId = version.Id, Requisition = requisition };
        var completedApproval = new RequisitionApproval
        {
            ApprovalWorkflowStage = stage1,
            StageOrder = 1,
            Status = RequisitionApprovalStatus.Approved,
            RequisitionApprovalProcess = process,
        };
        completedApproval.Actions.Add(new RequisitionApprovalAction { ActionType = ApprovalActionType.Approve });

        process.StageInstances.Add(completedApproval);

        _workflowRepository.Setup(r => r.GetVersionByIdAsync(version.Id, It.IsAny<CancellationToken>())).ReturnsAsync(version);
        _userRepository.Setup(r => r.GetByIdAsync(conditionalApprover, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = conditionalApprover, FullName = "Dept Head", IsActive = true, CompanyId = _companyId });

        await _engine.AdvanceAfterApprovalAsync(completedApproval, _requestorId, "Approver", "Manager", CancellationToken.None);

        // The pre-existing cost (25000) is in stage 2's >=20000 range, so stage 2 gets created (not
        // skipped) rather than the requisition jumping straight to Approved.
        requisition.EstimatedCost.Should().Be(25000m);
        requisition.Status.Should().Be(RequisitionStatus.UnderReview);
        _requisitionApprovalRepository.Verify(r => r.AddApproval(It.Is<RequisitionApproval>(a => a.StageOrder == 2)), Times.Once);
    }

    [Fact]
    public async Task AdvanceAfterApprovalAsync_MultiHopCostCondition_ExcludesStageBelowThreshold_EvenWhenCapturingStageInstanceIsNotLoaded()
    {
        // Regression for a real production bug: GetApprovalByIdAsync (the repository method that loads
        // the just-completed approval) never eager-loads RequisitionApprovalProcess.StageInstances, so
        // in production process.StageInstances only ever contains whichever approval row EF's own
        // relationship fixup happened to attach - never the FULL stage history. A now-removed
        // "costReliablyKnown" gate relied on scanning that under-populated collection for an earlier
        // CapturesEstimatedCost stage, which worked by coincidence on the very first hop (stage 1 IS the
        // row being evaluated) but silently broke on any later hop (e.g. stage 2 completing, evaluating
        // stage 3 - stage 1's row was never loaded here at all), letting a cost-conditional stage 3
        // through even when the requisition's real cost was below its MinCost. Cost conditions must be
        // evaluated directly against Requisition.EstimatedCost, with no dependency on StageInstances.
        var stage1 = NewStage(1, capturesCost: true);
        var stage2 = NewStage(2);
        stage2.Conditions.Add(new ApprovalWorkflowStageCondition { ConditionType = ApprovalConditionType.Cost, MinCost = 20001m, MaxCost = 100000m });
        var stage3 = NewStage(3);
        stage3.Conditions.Add(new ApprovalWorkflowStageCondition { ConditionType = ApprovalConditionType.Cost, MinCost = 100001m });

        var version = new ApprovalWorkflowVersion { Id = Guid.NewGuid(), IsPublished = true, AppliesToAllCategories = true };
        version.Stages.Add(stage1);
        version.Stages.Add(stage2);
        version.Stages.Add(stage3);

        var requisition = NewRequisition(estimatedCost: 100000m); // exactly at stage 2's ceiling, below stage 3's floor
        requisition.Status = RequisitionStatus.UnderReview;
        var process = new RequisitionApprovalProcess { RequisitionId = requisition.Id, ApprovalWorkflowVersionId = version.Id, Requisition = requisition };

        // Deliberately mirrors the production gap: only stage 2's own row is present on the process -
        // stage 1's (the CapturesEstimatedCost stage) is NOT, since GetApprovalByIdAsync never loaded it.
        var completedApproval = new RequisitionApproval
        {
            ApprovalWorkflowStage = stage2,
            StageOrder = 2,
            Status = RequisitionApprovalStatus.Approved,
            RequisitionApprovalProcess = process,
        };
        process.StageInstances.Add(completedApproval);

        _workflowRepository.Setup(r => r.GetVersionByIdAsync(version.Id, It.IsAny<CancellationToken>())).ReturnsAsync(version);

        await _engine.AdvanceAfterApprovalAsync(completedApproval, _requestorId, "Approver", "DepartmentHead", CancellationToken.None);

        // Stage 3 (MinCost 100001) must be excluded - 100000 < 100001 - so the requisition reaches
        // Approved instead of incorrectly waiting on an Executive Review stage.
        requisition.Status.Should().Be(RequisitionStatus.Approved);
        process.CompletedAtUtc.Should().NotBeNull();
        _requisitionApprovalRepository.Verify(r => r.AddApproval(It.Is<RequisitionApproval>(a => a.StageOrder == 3)), Times.Never);
    }

    /// <summary>Regression coverage for the real bug: Department Head's (Director Review) Cost
    /// condition used to be MinCost=20001/MaxCost=100000 - correct up to 100,000, but silently excluded
    /// above it (100000 &gt; MaxCost), so a high-value requisition jumped straight from Manager to CEO,
    /// skipping Department Head entirely. Corrected condition is open-ended: MinCost=20000.01, no
    /// MaxCost, so Department Head stays in the chain no matter how high the cost goes - cumulative
    /// escalation, not a price band. 20,000 exactly must NOT include it (Manager only); 20,000.01 and
    /// any amount far above 100,000 both must.</summary>
    [Theory]
    [InlineData(20000, false)]
    [InlineData(20000.01, true)]
    [InlineData(500000, true)] // the actual reported bug case - must NOT be skipped at high cost
    public async Task AdvanceAfterApprovalAsync_AfterManagerStage_DirectorReviewCondition_MatchesCorrectedThreshold(
        double costDouble, bool expectDirectorReviewCreated)
    {
        var cost = (decimal)costDouble;
        var deptHead = Guid.NewGuid();
        var stage1 = NewStage(1, capturesCost: true);
        var stage2 = NewStage(2, capturesCost: false);
        stage2.Name = "Director Review";
        stage2.Conditions.Add(new ApprovalWorkflowStageCondition { ConditionType = ApprovalConditionType.Cost, MinCost = 20000.01m });
        stage2.Approvers.Add(new WorkflowApprover { ApproverType = ApproverType.SpecificUser, ApproverUserId = deptHead, IsRequired = true });

        var version = new ApprovalWorkflowVersion { Id = Guid.NewGuid(), IsPublished = true, AppliesToAllCategories = true };
        version.Stages.Add(stage1);
        version.Stages.Add(stage2);

        var requisition = NewRequisition(estimatedCost: cost);
        requisition.Status = RequisitionStatus.UnderReview;
        var process = new RequisitionApprovalProcess { RequisitionId = requisition.Id, ApprovalWorkflowVersionId = version.Id, Requisition = requisition };
        var completedApproval = new RequisitionApproval
        {
            ApprovalWorkflowStage = stage1,
            StageOrder = 1,
            Status = RequisitionApprovalStatus.Approved,
            RequisitionApprovalProcess = process,
        };
        process.StageInstances.Add(completedApproval);

        _workflowRepository.Setup(r => r.GetVersionByIdAsync(version.Id, It.IsAny<CancellationToken>())).ReturnsAsync(version);
        _userRepository.Setup(r => r.GetByIdAsync(deptHead, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = deptHead, FullName = "Dept Head", IsActive = true, CompanyId = _companyId });

        await _engine.AdvanceAfterApprovalAsync(completedApproval, _requestorId, "Manager", "Manager", CancellationToken.None);

        var expectedTimes = expectDirectorReviewCreated ? Times.Once() : Times.Never();
        _requisitionApprovalRepository.Verify(r => r.AddApproval(It.Is<RequisitionApproval>(a => a.StageOrder == 2)), expectedTimes);
        requisition.Status.Should().Be(expectDirectorReviewCreated ? RequisitionStatus.UnderReview : RequisitionStatus.Approved);
    }

    /// <summary>Same corrected-threshold coverage one hop further in: CEO (Executive Review) must stay
    /// excluded at exactly 100,000 (Manager + Department Head only) and included from 100,000.01
    /// upward.</summary>
    [Theory]
    [InlineData(100000, false)]
    [InlineData(100000.01, true)]
    public async Task AdvanceAfterApprovalAsync_AfterDirectorReviewStage_ExecutiveReviewCondition_MatchesCorrectedThreshold(
        double costDouble, bool expectExecutiveReviewCreated)
    {
        var cost = (decimal)costDouble;
        var ceo = Guid.NewGuid();
        var stage1 = NewStage(1, capturesCost: true);
        var stage2 = NewStage(2, capturesCost: false);
        stage2.Name = "Director Review";
        stage2.Conditions.Add(new ApprovalWorkflowStageCondition { ConditionType = ApprovalConditionType.Cost, MinCost = 20000.01m });
        var stage3 = NewStage(3, capturesCost: false);
        stage3.Name = "Executive Review";
        stage3.Conditions.Add(new ApprovalWorkflowStageCondition { ConditionType = ApprovalConditionType.Cost, MinCost = 100000.01m });
        stage3.Approvers.Add(new WorkflowApprover { ApproverType = ApproverType.SpecificUser, ApproverUserId = ceo, IsRequired = true });

        var version = new ApprovalWorkflowVersion { Id = Guid.NewGuid(), IsPublished = true, AppliesToAllCategories = true };
        version.Stages.Add(stage1);
        version.Stages.Add(stage2);
        version.Stages.Add(stage3);

        var requisition = NewRequisition(estimatedCost: cost);
        requisition.Status = RequisitionStatus.UnderReview;
        var process = new RequisitionApprovalProcess { RequisitionId = requisition.Id, ApprovalWorkflowVersionId = version.Id, Requisition = requisition };
        var completedApproval = new RequisitionApproval
        {
            ApprovalWorkflowStage = stage2,
            StageOrder = 2,
            Status = RequisitionApprovalStatus.Approved,
            RequisitionApprovalProcess = process,
        };
        process.StageInstances.Add(completedApproval);

        _workflowRepository.Setup(r => r.GetVersionByIdAsync(version.Id, It.IsAny<CancellationToken>())).ReturnsAsync(version);
        _userRepository.Setup(r => r.GetByIdAsync(ceo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = ceo, FullName = "CEO", IsActive = true, CompanyId = _companyId });

        await _engine.AdvanceAfterApprovalAsync(completedApproval, _requestorId, "Dept Head", "DepartmentHead", CancellationToken.None);

        var expectedTimes = expectExecutiveReviewCreated ? Times.Once() : Times.Never();
        _requisitionApprovalRepository.Verify(r => r.AddApproval(It.Is<RequisitionApproval>(a => a.StageOrder == 3)), expectedTimes);
        requisition.Status.Should().Be(expectExecutiveReviewCreated ? RequisitionStatus.UnderReview : RequisitionStatus.Approved);
    }
}
