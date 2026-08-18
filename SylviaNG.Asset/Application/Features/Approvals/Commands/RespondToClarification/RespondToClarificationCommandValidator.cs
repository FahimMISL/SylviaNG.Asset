using FluentValidation;

namespace RMS.Application.Features.Approvals.Commands.RespondToClarification;

public class RespondToClarificationCommandValidator : AbstractValidator<RespondToClarificationCommand>
{
    public RespondToClarificationCommandValidator()
    {
        RuleFor(c => c.ApprovalId).NotEmpty();
    }
}
