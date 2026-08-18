using FluentValidation;

namespace RMS.Application.Features.Approvals.Commands.DelegateApprovalAction;

public class DelegateApprovalActionCommandValidator : AbstractValidator<DelegateApprovalActionCommand>
{
    public DelegateApprovalActionCommandValidator()
    {
        RuleFor(c => c.ApprovalId).NotEmpty();
        RuleFor(c => c.DelegateToUserId).NotEmpty();
    }
}
