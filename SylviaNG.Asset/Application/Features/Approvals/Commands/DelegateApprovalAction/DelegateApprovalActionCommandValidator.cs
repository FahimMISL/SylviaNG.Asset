using FluentValidation;
using RMS.Application.Features.Approvals.Services;

namespace RMS.Application.Features.Approvals.Commands.DelegateApprovalAction;

public class DelegateApprovalActionCommandValidator : AbstractValidator<DelegateApprovalActionCommand>
{
    public DelegateApprovalActionCommandValidator()
    {
        RuleFor(c => c.ApprovalId).NotEmpty();
        RuleFor(c => c.DelegateToUserId).NotEmpty();
        RuleFor(c => c.Comment).NotEmpty().MinimumLength(CommentValidation.MinimumLength)
            .WithMessage($"Comment must be at least {CommentValidation.MinimumLength} characters.");
    }
}
