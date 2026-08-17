using FluentValidation;

namespace RMS.Application.Features.CostCenters.Commands.UpdateCostCenter;

public class UpdateCostCenterCommandValidator : AbstractValidator<UpdateCostCenterCommand>
{
    public UpdateCostCenterCommandValidator()
    {
        RuleFor(c => c.CostCenterId).NotEmpty();
        RuleFor(c => c.Code).NotEmpty().MaximumLength(50);
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
    }
}
