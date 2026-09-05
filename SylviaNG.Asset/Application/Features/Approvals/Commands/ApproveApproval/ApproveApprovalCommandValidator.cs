using FluentValidation;

namespace RMS.Application.Features.Approvals.Commands.ApproveApproval;

/// <summary>Comment is optional here - forcing one on every approver action was premature.</summary>
public class ApproveApprovalCommandValidator : AbstractValidator<ApproveApprovalCommand>
{
    public ApproveApprovalCommandValidator()
    {
        RuleFor(c => c.ApprovalId).NotEmpty();
    }
}
