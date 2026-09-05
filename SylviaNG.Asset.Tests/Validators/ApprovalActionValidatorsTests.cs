using FluentAssertions;
using RMS.Application.Features.Approvals.Commands.ApproveApproval;
using RMS.Application.Features.Approvals.Commands.PartialApproveApproval;
using RMS.Application.Features.Approvals.Commands.RejectApproval;
using RMS.Application.Features.Approvals.DTOs;

namespace SylviaNG.Assets.Tests.Validators;

/// <summary>Comment and decline-reason are both optional on every approval action - requiring one
/// on every action was premature. See ApproveApprovalCommandValidator's remarks.</summary>
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
    public void RejectValidator_WithShortComment_HasNoError()
    {
        var validator = new RejectApprovalCommandValidator();
        var command = new RejectApprovalCommand(Guid.NewGuid(), "short");

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
}
