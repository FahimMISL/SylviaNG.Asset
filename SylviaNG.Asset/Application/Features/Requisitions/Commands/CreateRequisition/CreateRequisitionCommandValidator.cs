using FluentValidation;
using SylviaNG.Assets.SharedKernel.Utils;

namespace RMS.Application.Features.Requisitions.Commands.CreateRequisition;

public class CreateRequisitionCommandValidator : AbstractValidator<CreateRequisitionCommand>
{
    public CreateRequisitionCommandValidator()
    {
        RuleFor(c => c.CategoryId).NotEmpty();

        RuleForEach(c => c.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Quantity).GreaterThanOrEqualTo(0);
        });

        // Submitting has stricter requirements than saving a draft; a draft can
        // be left partially filled in. Justification is intentionally not
        // validated here at all: it is optional with no minimum length.
        // Item/CategoryItemId validity against the category is checked in the
        // handler (RequisitionFieldValidation.ResolveItems), since it needs the category record.
        When(c => c.Submit, () =>
        {
            RuleFor(c => c.Items).NotEmpty().WithMessage("Add at least one item before submitting.");
            RuleForEach(c => c.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.CategoryItemId).NotEmpty().WithMessage("Please select an item.");
                item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero.");
            });
            RuleFor(c => c.NeedByDate).NotEqual(default(DateTime)).WithMessage("Need By Date is required.");
            // Compared in the same reference frame NeedByDate itself was converted through
            // (LocalDateTimeJsonConverter treats incoming dates as local business time, not UTC) -
            // comparing against raw DateTime.UtcNow.Date here rejected "today" as if it were
            // already in the past whenever local time is ahead of UTC.
            RuleFor(c => c.NeedByDate)
                .GreaterThanOrEqualTo(DateTimeUtility.StartOfDayUtc(DateTimeUtility.TodayLocal()))
                .WithMessage("Need By Date cannot be in the past.");
        });
    }
}
