using FluentValidation;
using RMS.Application.Features.Approvals.Services;

namespace RMS.Application.Features.Approvals.Commands.CreateDelegation;

public class CreateDelegationCommandValidator : AbstractValidator<CreateDelegationCommand>
{
    public CreateDelegationCommandValidator()
    {
        RuleFor(c => c.DelegateUserId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty().MinimumLength(CommentValidation.MinimumLength).MaximumLength(500)
            .WithMessage($"Reason must be at least {CommentValidation.MinimumLength} characters.");
        RuleFor(c => c.EndDate).GreaterThanOrEqualTo(c => c.StartDate)
            .WithMessage("End date cannot be before the start date.");
    }
}
