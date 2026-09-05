using RMS.Application.Features.ApprovalWorkflows.DTOs;
using RMS.Domain.Entities;

namespace RMS.Application.Features.ApprovalWorkflows.Mappings;

/// <summary>Builds domain stage entities (with nested approvers/conditions/SLA) from the
/// request-shaped inputs. Shared by Create/Update/Clone handlers.</summary>
public static class ApprovalWorkflowStageMapper
{
    public static List<ApprovalWorkflowStage> ToEntities(IEnumerable<ApprovalWorkflowStageInput> inputs)
    {
        var result = new List<ApprovalWorkflowStage>();

        foreach (var input in inputs)
        {
            var stage = new ApprovalWorkflowStage
            {
                StageOrder = input.StageOrder,
                Name = input.Name,
                CapturesEstimatedCost = input.CapturesEstimatedCost,
            };

            foreach (var approver in input.Approvers)
            {
                stage.Approvers.Add(new WorkflowApprover
                {
                    ApprovalWorkflowStageId = stage.Id,
                    ApproverType = approver.ApproverType,
                    ApproverRole = approver.ApproverRole,
                    ApproverUserId = approver.ApproverUserId,
                    FallbackApproverUserId = approver.FallbackApproverUserId,
                    IsRequired = approver.IsRequired,
                });
            }

            foreach (var condition in input.Conditions)
            {
                stage.Conditions.Add(new ApprovalWorkflowStageCondition
                {
                    ApprovalWorkflowStageId = stage.Id,
                    ConditionType = condition.ConditionType,
                    MinCost = condition.MinCost,
                    MaxCost = condition.MaxCost,
                    CategoryId = condition.CategoryId,
                });
            }

            if (input.Sla is not null)
            {
                stage.Sla = new ApprovalWorkflowSlaConfiguration
                {
                    ApprovalWorkflowStageId = stage.Id,
                    DurationValue = input.Sla.DurationValue,
                    DurationUnit = input.Sla.DurationUnit,
                    Reminder50PercentEnabled = input.Sla.Reminder50PercentEnabled,
                    Reminder80PercentEnabled = input.Sla.Reminder80PercentEnabled,
                    EscalateOnBreach = input.Sla.EscalateOnBreach,
                    EscalationApproverRole = input.Sla.EscalationApproverRole,
                    EscalationApproverUserId = input.Sla.EscalationApproverUserId,
                };
            }

            result.Add(stage);
        }

        return result;
    }

    // A prior "no two Cost condition ranges may overlap" validation used to live here. Removed: it
    // rejected the exact shape a real cumulative/escalating approval chain requires - e.g. Department
    // Head applying to everything above 20,000 (open-ended, no MaxCost) *and* CEO applying to
    // everything above 100,000, which necessarily overlap above 100,000 since Department Head must
    // stay in the chain there too, not get replaced by CEO. ApprovalWorkflowEngine.IsStageApplicable
    // already evaluates each stage's conditions independently and fires every stage whose conditions
    // are satisfied, in StageOrder - overlapping Cost ranges across stages isn't ambiguous to it, it's
    // exactly how a multi-tier escalation is expressed. ApprovalWorkflowRoutingMode (Sequential/
    // Parallel/Conditional) doesn't change this - it's UI-label-only and never branches the engine.
}
