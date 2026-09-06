using FluentValidation;
using RMS.Application.Features.ApprovalWorkflows.Mappings;
using RMS.Domain.Enums;

namespace RMS.Application.Features.ApprovalWorkflows.Commands.UpdateApprovalWorkflowVersion;

public class UpdateApprovalWorkflowVersionCommandValidator : AbstractValidator<UpdateApprovalWorkflowVersionCommand>
{
    public UpdateApprovalWorkflowVersionCommandValidator()
    {
        RuleFor(c => c.Stages).NotEmpty().WithMessage("A workflow must have at least one stage.");

        RuleFor(c => c.Stages)
            .Must(stages => stages.Select(s => s.StageOrder).Distinct().Count() == stages.Count)
            .WithMessage("Stage order values must be unique.")
            .When(c => c.Stages.Count > 0);

        RuleFor(c => c.Stages)
            .Must(ApprovalWorkflowStageMapper.HasNoOverlappingCostRanges)
            .WithMessage("Two or more stages have overlapping Cost condition ranges.")
            .When(c => c.Stages.Count > 0);

        RuleForEach(c => c.Stages).ChildRules(stage =>
        {
            stage.RuleFor(s => s.Name).NotEmpty().MaximumLength(200);
            stage.RuleFor(s => s.Approvers).NotEmpty().WithMessage("Each stage needs at least one approver.");

            stage.RuleForEach(s => s.Approvers).ChildRules(approver =>
            {
                approver.RuleFor(a => a.ApproverRole).NotNull()
                    .When(a => a.ApproverType == ApproverType.Role)
                    .WithMessage("A Role-type approver must specify a role.");
                approver.RuleFor(a => a.ApproverUserId).NotNull()
                    .When(a => a.ApproverType == ApproverType.SpecificUser)
                    .WithMessage("A Specific User approver must specify a user.");
            });

            stage.RuleForEach(s => s.Conditions).ChildRules(condition =>
            {
                condition.RuleFor(c => c.CategoryId).NotNull()
                    .When(c => c.ConditionType == ApprovalConditionType.Category)
                    .WithMessage("A Category condition must specify a category.");
                condition.RuleFor(c => c).Must(c => c.MinCost.HasValue || c.MaxCost.HasValue)
                    .When(c => c.ConditionType == ApprovalConditionType.Cost)
                    .WithMessage("A Cost condition must specify at least a minimum or maximum.");
            });

            stage.RuleFor(s => s.Sla!.DurationValue).GreaterThan(0).When(s => s.Sla is not null);
        });

        RuleFor(c => c.CategoryIds).NotEmpty()
            .When(c => !c.AppliesToAllCategories)
            .WithMessage("Select at least one category, or apply this workflow to all categories.");
    }
}
