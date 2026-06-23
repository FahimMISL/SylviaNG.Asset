using FluentValidation;

namespace SylviaNG.Assets.Application.Features.Assets.Commands.AssetUpdate
{
    public class AssetUpdateValidator : AbstractValidator<AssetUpdateCommand>
    {
        public AssetUpdateValidator()
        {
            RuleFor(x => x.AssetId)
                .GreaterThan(0).WithMessage("AssetId is required.");

            RuleFor(x => x.Request.Name)
                .MaximumLength(200).WithMessage("Name must not exceed 200 characters.")
                .When(x => x.Request.Name != null);

            RuleFor(x => x.Request.AssetCode)
                .MaximumLength(50).WithMessage("AssetCode must not exceed 50 characters.")
                .When(x => x.Request.AssetCode != null);
        }
    }
}