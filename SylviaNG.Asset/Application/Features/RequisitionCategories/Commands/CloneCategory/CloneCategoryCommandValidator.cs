using FluentValidation;

namespace RMS.Application.Features.RequisitionCategories.Commands.CloneCategory;

public class CloneCategoryCommandValidator : AbstractValidator<CloneCategoryCommand>
{
    public CloneCategoryCommandValidator()
    {
        RuleFor(c => c.SourceCategoryId).NotEmpty();
        RuleFor(c => c.NewName).NotEmpty().MaximumLength(200);
    }
}
