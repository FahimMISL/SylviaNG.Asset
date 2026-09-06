using FluentValidation;
using RMS.Application.Features.RequisitionCategories.Validators;

namespace RMS.Application.Features.RequisitionCategories.Commands.UpdateCategory;

public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(c => c.CategoryId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).MaximumLength(2000);
        RuleFor(c => c.ProjectCodeRequirement).IsInEnum();
        RuleForEach(c => c.FieldDefinitions).SetValidator(new FieldDefinitionInputValidator());
    }
}
