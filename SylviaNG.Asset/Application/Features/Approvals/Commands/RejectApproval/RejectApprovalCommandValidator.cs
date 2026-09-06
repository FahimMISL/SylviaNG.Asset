using FluentValidation;

namespace RMS.Application.Features.Approvals.Commands.RejectApproval;

public class RejectApprovalCommandValidator : AbstractValidator<RejectApprovalCommand>
{
    public RejectApprovalCommandValidator()
    {
        RuleFor(c => c.ApprovalId).NotEmpty();
    }
}
