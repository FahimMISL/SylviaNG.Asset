using FluentValidation;

namespace RMS.Application.Features.Requisitions.Commands.DeleteRequisition;

public class DeleteRequisitionCommandValidator : AbstractValidator<DeleteRequisitionCommand>
{
    public DeleteRequisitionCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
    }
}
