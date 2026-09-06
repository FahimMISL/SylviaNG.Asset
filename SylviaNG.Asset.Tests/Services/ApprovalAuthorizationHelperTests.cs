using FluentAssertions;
using Moq;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Approvals.Services;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace SylviaNG.Assets.Tests.Services;

public class ApprovalAuthorizationHelperTests
{
    private readonly Mock<IApprovalDelegationRepository> _delegationRepository = new();

    [Fact]
    public async Task GetActionableAssignmentAsync_CurrentUserIsDirectAssignee_ReturnsAssignment()
    {
        var userId = Guid.NewGuid();
        var approval = new RequisitionApproval();
        var assignment = new RequisitionApprovalAssignment { AssignedUserId = userId, HasActed = false };
        approval.Assignments.Add(assignment);

        _delegationRepository
            .Setup(r => r.GetActiveOnAsync(userId, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApprovalDelegation?)null);

        var result = await ApprovalAuthorizationHelper.GetActionableAssignmentAsync(approval, userId, _delegationRepository.Object, CancellationToken.None);

        result.Should().BeSameAs(assignment);
    }

    [Fact]
    public async Task GetActionableAssignmentAsync_UnrelatedUser_ThrowsForbidden()
    {
        var assignedUserId = Guid.NewGuid();
        var unrelatedUserId = Guid.NewGuid();
        var approval = new RequisitionApproval();
        approval.Assignments.Add(new RequisitionApprovalAssignment { AssignedUserId = assignedUserId, HasActed = false });

        _delegationRepository
            .Setup(r => r.GetActiveOnAsync(assignedUserId, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApprovalDelegation?)null);

        var act = () => ApprovalAuthorizationHelper.GetActionableAssignmentAsync(approval, unrelatedUserId, _delegationRepository.Object, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetActionableAssignmentAsync_DelegateOfAssignee_ReturnsAssignment()
    {
        // Out-of-office ApprovalDelegation: the delegate can act even though AssignedUserId still
        // points at the original approver - the row itself is never mutated.
        var originalApproverId = Guid.NewGuid();
        var delegateId = Guid.NewGuid();
        var approval = new RequisitionApproval();
        var assignment = new RequisitionApprovalAssignment { AssignedUserId = originalApproverId, HasActed = false };
        approval.Assignments.Add(assignment);

        _delegationRepository
            .Setup(r => r.GetActiveOnAsync(originalApproverId, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApprovalDelegation { DelegatorUserId = originalApproverId, DelegateUserId = delegateId });

        var result = await ApprovalAuthorizationHelper.GetActionableAssignmentAsync(approval, delegateId, _delegationRepository.Object, CancellationToken.None);

        result.Should().BeSameAs(assignment);
    }

    [Fact]
    public async Task GetActionableAssignmentAsync_OriginalApproverDuringActiveDelegation_ThrowsForbidden()
    {
        // Once delegated (today), only the resolved effective assignee (the delegate) can act - the
        // original approver can still VIEW (a separate, read-only path), but not act.
        var originalApproverId = Guid.NewGuid();
        var delegateId = Guid.NewGuid();
        var approval = new RequisitionApproval();
        approval.Assignments.Add(new RequisitionApprovalAssignment { AssignedUserId = originalApproverId, HasActed = false });

        _delegationRepository
            .Setup(r => r.GetActiveOnAsync(originalApproverId, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApprovalDelegation { DelegatorUserId = originalApproverId, DelegateUserId = delegateId });

        var act = () => ApprovalAuthorizationHelper.GetActionableAssignmentAsync(approval, originalApproverId, _delegationRepository.Object, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetActionableAssignmentAsync_AlreadyActedAssignment_IsSkipped_ThrowsForbidden()
    {
        // Double-action guard at the authorization layer: an assignment that already acted is not
        // "actionable" again by the same or any other user resolving to it.
        var userId = Guid.NewGuid();
        var approval = new RequisitionApproval();
        approval.Assignments.Add(new RequisitionApprovalAssignment { AssignedUserId = userId, HasActed = true, ActedAtUtc = DateTime.UtcNow });

        var act = () => ApprovalAuthorizationHelper.GetActionableAssignmentAsync(approval, userId, _delegationRepository.Object, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    // --- IsCurrentUserActionableAsync: the RequisitionDto.ApprovalProcess.CurrentUserCanAct signal ---

    private static RequisitionApprovalProcess NewProcessWithCurrentStage(RequisitionApprovalStatus status, int currentStageOrder,
        out RequisitionApproval currentApproval)
    {
        var process = new RequisitionApprovalProcess { CurrentStageOrder = currentStageOrder };
        currentApproval = new RequisitionApproval { StageOrder = currentStageOrder, Status = status };
        process.StageInstances.Add(currentApproval);
        return process;
    }

    [Fact]
    public async Task IsCurrentUserActionableAsync_DirectAssignee_ReturnsTrue()
    {
        var userId = Guid.NewGuid();
        var process = NewProcessWithCurrentStage(RequisitionApprovalStatus.Pending, 1, out var approval);
        approval.Assignments.Add(new RequisitionApprovalAssignment { AssignedUserId = userId, HasActed = false });

        _delegationRepository
            .Setup(r => r.GetActiveOnAsync(userId, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApprovalDelegation?)null);

        var result = await ApprovalAuthorizationHelper.IsCurrentUserActionableAsync(process, userId, _delegationRepository.Object, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsCurrentUserActionableAsync_ActiveDelegateViewingDirectly_ReturnsTrue()
    {
        // The exact gap the frontend hit: an out-of-office delegate opening the requisition detail
        // page directly (not via the inbox) must still see this as true - same resolution as the
        // real action handlers use.
        var originalApproverId = Guid.NewGuid();
        var delegateId = Guid.NewGuid();
        var process = NewProcessWithCurrentStage(RequisitionApprovalStatus.InProgress, 1, out var approval);
        approval.Assignments.Add(new RequisitionApprovalAssignment { AssignedUserId = originalApproverId, HasActed = false });

        _delegationRepository
            .Setup(r => r.GetActiveOnAsync(originalApproverId, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApprovalDelegation { DelegatorUserId = originalApproverId, DelegateUserId = delegateId });

        var result = await ApprovalAuthorizationHelper.IsCurrentUserActionableAsync(process, delegateId, _delegationRepository.Object, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsCurrentUserActionableAsync_UnrelatedUser_ReturnsFalse()
    {
        var process = NewProcessWithCurrentStage(RequisitionApprovalStatus.Pending, 1, out var approval);
        approval.Assignments.Add(new RequisitionApprovalAssignment { AssignedUserId = Guid.NewGuid(), HasActed = false });

        _delegationRepository
            .Setup(r => r.GetActiveOnAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApprovalDelegation?)null);

        var result = await ApprovalAuthorizationHelper.IsCurrentUserActionableAsync(process, Guid.NewGuid(), _delegationRepository.Object, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsCurrentUserActionableAsync_NullProcess_ReturnsFalse()
    {
        var result = await ApprovalAuthorizationHelper.IsCurrentUserActionableAsync(null, Guid.NewGuid(), _delegationRepository.Object, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsCurrentUserActionableAsync_ProcessTerminal_ReturnsFalse()
    {
        var process = new RequisitionApprovalProcess { CurrentStageOrder = null, CompletedAtUtc = DateTime.UtcNow };

        var result = await ApprovalAuthorizationHelper.IsCurrentUserActionableAsync(process, Guid.NewGuid(), _delegationRepository.Object, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsCurrentUserActionableAsync_CurrentStageAwaitingClarification_ReturnsFalse()
    {
        // Nobody can act on an approver action while ClarificationRequested is outstanding - the ball
        // is in the requestor's court (RespondToClarification, a separate requestor-only path).
        var userId = Guid.NewGuid();
        var process = NewProcessWithCurrentStage(RequisitionApprovalStatus.ClarificationRequested, 1, out var approval);
        approval.Assignments.Add(new RequisitionApprovalAssignment { AssignedUserId = userId, HasActed = false });

        var result = await ApprovalAuthorizationHelper.IsCurrentUserActionableAsync(process, userId, _delegationRepository.Object, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsCurrentUserActionableAsync_AlreadyActedAssignment_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        var process = NewProcessWithCurrentStage(RequisitionApprovalStatus.InProgress, 1, out var approval);
        approval.Assignments.Add(new RequisitionApprovalAssignment { AssignedUserId = userId, HasActed = true, ActedAtUtc = DateTime.UtcNow });

        var result = await ApprovalAuthorizationHelper.IsCurrentUserActionableAsync(process, userId, _delegationRepository.Object, CancellationToken.None);

        result.Should().BeFalse();
    }
}
