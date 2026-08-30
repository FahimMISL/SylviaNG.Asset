using FluentValidation;

namespace RMS.Application.Features.Procurement.Commands.RecordFulfillment;

public class RecordFulfillmentCommandValidator : AbstractValidator<RecordFulfillmentCommand>
{
    public RecordFulfillmentCommandValidator()
    {
        RuleFor(c => c.Items).NotEmpty().WithMessage("Record at least one item's fulfilled quantity.");
        RuleForEach(c => c.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Quantity).GreaterThanOrEqualTo(0).WithMessage("Quantity cannot be negative.");
        });
    }
}
