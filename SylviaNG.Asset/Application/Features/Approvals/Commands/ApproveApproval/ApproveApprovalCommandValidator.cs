using FluentValidation;

namespace RMS.Application.Features.Approvals.Commands.ApproveApproval;

/// <summary>Comment and EstimatedCost are both optional here: there's no Procurement Officer
/// feature yet to be the real source of a cost figure, and forcing a comment/cost on every
/// approver action was premature ahead of that. If a cost IS provided, it still has to be a
/// sane positive number.</summary>
public class ApproveApprovalCommandValidator : AbstractValidator<ApproveApprovalCommand>
{
    public ApproveApprovalCommandValidator()
    {
        RuleFor(c => c.ApprovalId).NotEmpty();
        RuleFor(c => c.EstimatedCost).GreaterThan(0).When(c => c.EstimatedCost.HasValue)
            .WithMessage("Estimated cost must be greater than zero.");
    }
}
