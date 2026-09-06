using FluentValidation;
using RMS.Application.Features.Approvals.Services;

namespace RMS.Application.Features.Approvals.Commands.RevokeDelegation;

public class RevokeDelegationCommandValidator : AbstractValidator<RevokeDelegationCommand>
{
    public RevokeDelegationCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty().MinimumLength(CommentValidation.MinimumLength)
            .WithMessage($"Reason must be at least {CommentValidation.MinimumLength} characters.");
    }
}
