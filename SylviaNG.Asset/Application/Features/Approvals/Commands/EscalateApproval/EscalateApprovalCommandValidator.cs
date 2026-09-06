using FluentValidation;
using RMS.Application.Features.Approvals.Services;

namespace RMS.Application.Features.Approvals.Commands.EscalateApproval;

public class EscalateApprovalCommandValidator : AbstractValidator<EscalateApprovalCommand>
{
    public EscalateApprovalCommandValidator()
    {
        RuleFor(c => c.ApprovalId).NotEmpty();
        RuleFor(c => c.Comment).NotEmpty().MinimumLength(CommentValidation.MinimumLength)
            .WithMessage($"Comment must be at least {CommentValidation.MinimumLength} characters.");
    }
}
