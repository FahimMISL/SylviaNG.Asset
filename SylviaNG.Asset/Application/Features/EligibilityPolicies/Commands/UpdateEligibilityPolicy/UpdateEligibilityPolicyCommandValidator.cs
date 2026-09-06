using FluentValidation;

namespace RMS.Application.Features.EligibilityPolicies.Commands.UpdateEligibilityPolicy;

public class UpdateEligibilityPolicyCommandValidator : AbstractValidator<UpdateEligibilityPolicyCommand>
{
    public UpdateEligibilityPolicyCommandValidator()
    {
        RuleFor(c => c.PolicyId).NotEmpty();
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
