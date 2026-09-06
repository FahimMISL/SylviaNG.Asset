using FluentAssertions;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace SylviaNG.Assets.Tests.Domain;

public class RequisitionApprovalTransitionsTests
{
    private static Requisition NewRequisition(RequisitionStatus status)
    {
        var requisition = new Requisition
        {
            CompanyId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            RequestedByUserId = Guid.NewGuid(),
            NeedByDate = DateTime.UtcNow.AddDays(7),
            Status = status,
        };
        return requisition;
    }

    [Fact]
    public void BeginReview_FromSubmitted_TransitionsToUnderReview_AndAppendsHistory()
    {
        var requisition = NewRequisition(RequisitionStatus.Submitted);
        var actorId = Guid.NewGuid();

        var entry = requisition.BeginReview(actorId, "Alice", "Employee");

        requisition.Status.Should().Be(RequisitionStatus.UnderReview);
        entry.FromStatus.Should().Be(RequisitionStatus.Submitted);
        entry.ToStatus.Should().Be(RequisitionStatus.UnderReview);
        requisition.StatusHistory.Should().ContainSingle().Which.Should().BeSameAs(entry);
    }

    [Fact]
    public void BeginReview_FromDraft_Throws()
    {
        var requisition = NewRequisition(RequisitionStatus.Draft);

        var act = () => requisition.BeginReview(Guid.NewGuid(), "Alice", "Employee");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Approve_FromUnderReview_TransitionsToApproved()
    {
        var requisition = NewRequisition(RequisitionStatus.UnderReview);

        var entry = requisition.Approve(Guid.NewGuid(), "System", null, "All stages completed.");

        requisition.Status.Should().Be(RequisitionStatus.Approved);
        entry.ToStatus.Should().Be(RequisitionStatus.Approved);
    }

    [Fact]
    public void Reject_FromUnderReview_TransitionsToRejected()
    {
        var requisition = NewRequisition(RequisitionStatus.UnderReview);

        var entry = requisition.Reject(Guid.NewGuid(), "Bob", "Manager", "Budget not available this quarter.");

        requisition.Status.Should().Be(RequisitionStatus.Rejected);
        entry.Comment.Should().Be("Budget not available this quarter.");
    }

    [Fact]
    public void SendBack_FromUnderReview_TransitionsToSentBack()
    {
        var requisition = NewRequisition(RequisitionStatus.UnderReview);

        var entry = requisition.SendBack(Guid.NewGuid(), "Bob", "Manager", "Please add more item detail.");

        requisition.Status.Should().Be(RequisitionStatus.SentBack);
        entry.ToStatus.Should().Be(RequisitionStatus.SentBack);
    }

    [Fact]
    public void PartialApprove_FromUnderReview_TransitionsToPartiallyApproved()
    {
        var requisition = NewRequisition(RequisitionStatus.UnderReview);

        var entry = requisition.PartialApprove(Guid.NewGuid(), "Carol", "DepartmentHead", "Approved 2 of 3 items.");

        requisition.Status.Should().Be(RequisitionStatus.PartiallyApproved);
        entry.ToStatus.Should().Be(RequisitionStatus.PartiallyApproved);
    }

    [Theory]
    [InlineData(RequisitionStatus.Draft)]
    [InlineData(RequisitionStatus.Approved)]
    [InlineData(RequisitionStatus.Rejected)]
    public void Approve_FromNonUnderReviewStatus_Throws(RequisitionStatus status)
    {
        var requisition = NewRequisition(status);

        var act = () => requisition.Approve(Guid.NewGuid(), "System", null, "x");

        act.Should().Throw<InvalidOperationException>();
    }

    // FR-RR-008/BR-RR-002/BR-RR-003: Cancel used to be blocked the instant a requisition reached
    // UnderReview, even though ApprovalWorkflowEngine.ResolveAndStartAsync moves Submitted ->
    // UnderReview automatically, in the same request as submission - before any human approver has
    // acted. That made cancellation effectively unreachable for any category with a resolvable
    // workflow. These tests cover the fix: Cancel (and CanCancel) must still work from UnderReview as
    // long as no stage instance has recorded an approver action yet, and must still be blocked once one has.

    [Fact]
    public void Cancel_FromSubmitted_TransitionsToCancelled()
    {
        var requisition = NewRequisition(RequisitionStatus.Submitted);

        var entry = requisition.Cancel(Guid.NewGuid(), "Alice", "Employee", "Changed my mind.");

        requisition.Status.Should().Be(RequisitionStatus.Cancelled);
        entry.ToStatus.Should().Be(RequisitionStatus.Cancelled);
    }

    [Fact]
    public void CanCancel_FromUnderReview_WithNoApprovalProcess_IsTrue()
    {
        var requisition = NewRequisition(RequisitionStatus.UnderReview);

        requisition.CanCancel.Should().BeTrue();
    }

    [Fact]
    public void Cancel_FromUnderReview_BeforeAnyApproverAction_TransitionsToCancelled()
    {
        var requisition = NewRequisition(RequisitionStatus.UnderReview);
        requisition.ApprovalProcess = new RequisitionApprovalProcess
        {
            RequisitionId = requisition.Id,
            CurrentStageOrder = 1,
            StageInstances =
            [
                new RequisitionApproval { StageOrder = 1, Status = RequisitionApprovalStatus.Pending },
            ],
        };

        requisition.CanCancel.Should().BeTrue();
        var entry = requisition.Cancel(Guid.NewGuid(), "Alice", "Employee", "Changed my mind.");

        requisition.Status.Should().Be(RequisitionStatus.Cancelled);
        entry.ToStatus.Should().Be(RequisitionStatus.Cancelled);
    }

    [Fact]
    public void Cancel_FromUnderReview_AfterAnApproverHasActed_Throws()
    {
        var requisition = NewRequisition(RequisitionStatus.UnderReview);
        requisition.ApprovalProcess = new RequisitionApprovalProcess
        {
            RequisitionId = requisition.Id,
            CurrentStageOrder = 1,
            StageInstances =
            [
                new RequisitionApproval
                {
                    StageOrder = 1,
                    Status = RequisitionApprovalStatus.InProgress,
                    Actions = [new RequisitionApprovalAction { ActionType = ApprovalActionType.Approve, ActorName = "Bob" }],
                },
            ],
        };

        requisition.CanCancel.Should().BeFalse();
        var act = () => requisition.Cancel(Guid.NewGuid(), "Alice", "Employee", "Too late.");

        act.Should().Throw<InvalidOperationException>();
    }
}
