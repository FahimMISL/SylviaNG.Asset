using FluentValidation;

namespace RMS.Application.Features.EligibilityPolicies.Commands.CreateEligibilityPolicy;

public class CreateEligibilityPolicyCommandValidator : AbstractValidator<CreateEligibilityPolicyCommand>
{
    public CreateEligibilityPolicyCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).MaximumLength(2000);
        RuleFor(c => c.CategoryId).NotEmpty();

        RuleForEach(c => c.Criteria).ChildRules(criterion =>
        {
            criterion.RuleFor(c => c.AllowedValue).NotEmpty().MaximumLength(100);
        });

        RuleFor(c => c.ReplacementRule!.DurationValue).GreaterThan(0).When(c => c.ReplacementRule is not null);
    }
}
