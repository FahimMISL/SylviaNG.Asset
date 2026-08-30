using FluentValidation;

namespace RMS.Application.Features.Approvals.Commands.PartialApproveApproval;

public class PartialApproveApprovalCommandValidator : AbstractValidator<PartialApproveApprovalCommand>
{
    public PartialApproveApprovalCommandValidator()
    {
        RuleFor(c => c.ApprovalId).NotEmpty();
        RuleFor(c => c.Decisions).NotEmpty().WithMessage("At least one item decision is required.");

        RuleForEach(c => c.Decisions).ChildRules(decision =>
        {
            decision.RuleFor(d => d.ApprovedQuantity).GreaterThanOrEqualTo(0);
            decision.RuleFor(d => d.DeclinedQuantity).GreaterThanOrEqualTo(0);
        });
    }
}
