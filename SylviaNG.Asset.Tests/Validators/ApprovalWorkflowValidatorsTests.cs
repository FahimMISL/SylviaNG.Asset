using FluentAssertions;
using RMS.Application.Features.ApprovalWorkflows.Commands.CreateApprovalWorkflow;
using RMS.Application.Features.ApprovalWorkflows.DTOs;
using RMS.Domain.Enums;

namespace SylviaNG.Assets.Tests.Validators;

public class ApprovalWorkflowValidatorsTests
{
    private readonly CreateApprovalWorkflowCommandValidator _validator = new();

    private static ApprovalWorkflowStageInput ValidStage(int order) => new(
        order, $"Stage {order}", false,
        [new WorkflowApproverInput(ApproverType.SpecificUser, null, Guid.NewGuid(), null, true)],
        [], null);

    private CreateApprovalWorkflowCommand ValidCommand(List<ApprovalWorkflowStageInput> stages) => new(
        "Standard Procurement", "desc", ApprovalWorkflowRoutingMode.Sequential, true, [], stages, null);

    [Fact]
    public void Validate_WithValidSingleStage_HasNoErrors()
    {
        var command = ValidCommand([ValidStage(1)]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithZeroStages_HasError()
    {
        // Zero-stage workflow rejected - a workflow that could never route anything must not be creatable.
        var command = ValidCommand([]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Stages");
    }

    [Fact]
    public void Validate_WithDuplicateStageOrders_HasError()
    {
        var command = ValidCommand([ValidStage(1), ValidStage(1)]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithStageWithNoApprovers_HasError()
    {
        var stage = new ApprovalWorkflowStageInput(1, "Stage 1", false, [], [], null);
        var command = ValidCommand([stage]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithRoleApproverMissingRole_HasError()
    {
        var stage = new ApprovalWorkflowStageInput(
            1, "Stage 1", false,
            [new WorkflowApproverInput(ApproverType.Role, null, null, null, true)],
            [], null);
        var command = ValidCommand([stage]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    /// <summary>Overlapping Cost ranges across stages used to be rejected outright, on the assumption
    /// that Cost conditions always partition into mutually-exclusive bands. That's wrong for a real
    /// cumulative escalation chain (Manager always -> Department Head above 20,000 -> CEO above
    /// 100,000): Department Head's range must stay open-ended above 20,000, which necessarily overlaps
    /// CEO's range above 100,000, since Department Head must remain in the chain there too, not get
    /// replaced by CEO. ApprovalWorkflowEngine.IsStageApplicable evaluates each stage's conditions
    /// independently and fires every stage whose conditions are satisfied, in StageOrder - overlap
    /// isn't ambiguous to it.</summary>
    [Fact]
    public void Validate_WithOverlappingCostRanges_IsAllowed_ForCumulativeEscalation()
    {
        var stage1 = new ApprovalWorkflowStageInput(
            1, "Stage 1", false,
            [new WorkflowApproverInput(ApproverType.SpecificUser, null, Guid.NewGuid(), null, true)],
            [new ApprovalWorkflowStageConditionInput(ApprovalConditionType.Cost, 20000.01m, null, null)], null);
        var stage2 = new ApprovalWorkflowStageInput(
            2, "Stage 2", false,
            [new WorkflowApproverInput(ApproverType.SpecificUser, null, Guid.NewGuid(), null, true)],
            [new ApprovalWorkflowStageConditionInput(ApprovalConditionType.Cost, 100000.01m, null, null)], null);
        var command = ValidCommand([stage1, stage2]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithNonOverlappingCostRanges_HasNoErrors()
    {
        var stage1 = new ApprovalWorkflowStageInput(
            1, "Stage 1", false,
            [new WorkflowApproverInput(ApproverType.SpecificUser, null, Guid.NewGuid(), null, true)],
            [new ApprovalWorkflowStageConditionInput(ApprovalConditionType.Cost, 0m, 10000m, null)], null);
        var stage2 = new ApprovalWorkflowStageInput(
            2, "Stage 2", false,
            [new WorkflowApproverInput(ApproverType.SpecificUser, null, Guid.NewGuid(), null, true)],
            [new ApprovalWorkflowStageConditionInput(ApprovalConditionType.Cost, 10001m, 30000m, null)], null);
        var command = ValidCommand([stage1, stage2]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
