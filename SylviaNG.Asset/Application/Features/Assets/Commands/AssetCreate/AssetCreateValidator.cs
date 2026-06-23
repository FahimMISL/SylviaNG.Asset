using FluentValidation;

namespace SylviaNG.Assets.Application.Features.Assets.Commands.AssetCreate
{
    public class AssetCreateValidator : AbstractValidator<AssetCreateCommand>
    {
        public AssetCreateValidator()
        {
            RuleFor(x => x.Request.AssetCode)
                .NotEmpty().WithMessage("AssetCode is required.")
                .MaximumLength(50).WithMessage("AssetCode must not exceed 50 characters.");

            RuleFor(x => x.Request.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

            RuleFor(x => x.Request.SiteId)
                .GreaterThan(0).WithMessage("SiteId is required.");

            RuleFor(x => x.Request.PurchaseValue)
                .GreaterThanOrEqualTo(0).When(x => x.Request.PurchaseValue.HasValue)
                .WithMessage("PurchaseValue cannot be negative.");
        }
    }
}