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

    /// <summary>The plan's overlapping-cost-range validation: no two Cost condition ranges across the
    /// whole set of stages may overlap (Min/Max null = open-ended). Prevents an ambiguous workflow
    /// where two different stages' cost thresholds both silently claim the same requisition amounts.</summary>
    public static bool HasNoOverlappingCostRanges(IEnumerable<ApprovalWorkflowStageInput> stages)
    {
        var ranges = stages
            .SelectMany(s => s.Conditions)
            .Where(c => c.ConditionType == Domain.Enums.ApprovalConditionType.Cost)
            .Select(c => (Min: c.MinCost ?? decimal.MinValue, Max: c.MaxCost ?? decimal.MaxValue))
            .ToList();

        for (var i = 0; i < ranges.Count; i++)
        {
            for (var j = i + 1; j < ranges.Count; j++)
            {
                if (ranges[i].Min <= ranges[j].Max && ranges[j].Min <= ranges[i].Max)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
