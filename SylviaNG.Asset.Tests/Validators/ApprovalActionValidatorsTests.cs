using FluentAssertions;
using RMS.Application.Features.Approvals.Commands.ApproveApproval;
using RMS.Application.Features.Approvals.Commands.PartialApproveApproval;
using RMS.Application.Features.Approvals.Commands.RejectApproval;
using RMS.Application.Features.Approvals.DTOs;

namespace SylviaNG.Assets.Tests.Validators;

/// <summary>Comment is intentionally optional on Approve and PartialApprove - a partial approval
/// already records its outcome in the per-item decisions, so a separate justification comment adds
/// friction without adding information (see PartialApproveApprovalCommandValidator's remarks). Every
/// other approval action (Reject, SendBack, RequestClarification, RespondToClarification,
/// DelegateApprovalAction, Escalate) requires a real explanation, same as CreateDelegation's Reason -
/// this was previously unenforced (empty/1-character comments were accepted) until that fix.
/// Decline-reason on a PartialApprove item stays optional regardless.</summary>
public class ApprovalActionValidatorsTests
{
    [Theory]
    [InlineData("short")]
    [InlineData("")]
    [InlineData(null)]
    public void ApproveValidator_WithAnyLengthComment_HasNoCommentError(string? comment)
    {
        var validator = new ApproveApprovalCommandValidator();
        var command = new ApproveApprovalCommand(Guid.NewGuid(), comment!);

        var result = validator.Validate(command);

        result.Errors.Should().NotContain(e => e.PropertyName == "Comment");
    }

    [Fact]
    public void RejectValidator_WithShortComment_HasError()
    {
        var validator = new RejectApprovalCommandValidator();
        var command = new RejectApprovalCommand(Guid.NewGuid(), "short");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Comment");
    }

    [Fact]
    public void RejectValidator_WithValidComment_HasNoError()
    {
        var validator = new RejectApprovalCommandValidator();
        var command = new RejectApprovalCommand(Guid.NewGuid(), "Budget for this item was already exhausted this quarter.");

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void PartialApproveValidator_WithDeclinedQuantityAndNoReason_HasNoError()
    {
        var validator = new PartialApproveApprovalCommandValidator();
        var command = new PartialApproveApprovalCommand(
            Guid.NewGuid(), "Approving most items, declining one.",
            [new PartialApprovalDecisionInput(Guid.NewGuid(), 2, 1, null)]);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void PartialApproveValidator_WithDeclinedQuantityAndReason_HasNoErrors()
    {
        var validator = new PartialApproveApprovalCommandValidator();
        var command = new PartialApproveApprovalCommand(
            Guid.NewGuid(), "Approving most items, declining one.",
            [new PartialApprovalDecisionInput(Guid.NewGuid(), 2, 1, "Out of stock at the moment.")]);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void PartialApproveValidator_WithNoDecisions_HasError()
    {
        var validator = new PartialApproveApprovalCommandValidator();
        var command = new PartialApproveApprovalCommand(Guid.NewGuid(), "Approving most items, declining one.", []);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("OK")]
    [InlineData("Approved")]
    public void PartialApproveValidator_WithAnyLengthComment_HasNoCommentError(string? comment)
    {
        var validator = new PartialApproveApprovalCommandValidator();
        var command = new PartialApproveApprovalCommand(
            Guid.NewGuid(), comment!,
            [new PartialApprovalDecisionInput(Guid.NewGuid(), 6, 4, null)]);

        var result = validator.Validate(command);

        result.Errors.Should().NotContain(e => e.PropertyName == "Comment");
    }
}
