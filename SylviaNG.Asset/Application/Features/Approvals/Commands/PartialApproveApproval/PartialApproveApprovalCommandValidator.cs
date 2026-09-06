using FluentValidation;

namespace RMS.Application.Features.Approvals.Commands.PartialApproveApproval;

/// <summary>Comment is intentionally optional here, same as ApproveApprovalCommandValidator - a
/// partial approval already records its outcome in the per-item decisions (and their own optional
/// DeclineReason), so forcing a separate 10-character justification comment on top added friction
/// without adding information and blocked an otherwise fully valid partial approval.</summary>
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
