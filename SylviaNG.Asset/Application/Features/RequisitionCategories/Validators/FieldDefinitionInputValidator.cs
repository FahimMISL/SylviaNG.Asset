using FluentValidation;
using RMS.Application.Features.RequisitionCategories.DTOs;
using RMS.Domain.Enums;

namespace RMS.Application.Features.RequisitionCategories.Validators;

/// <summary>Shared field-level rules reused by Create and Update category validators.</summary>
public class FieldDefinitionInputValidator : AbstractValidator<FieldDefinitionInput>
{
    public FieldDefinitionInputValidator()
    {
        RuleFor(f => f.Label).NotEmpty().MaximumLength(200);
        RuleFor(f => f.DisplayOrder).GreaterThanOrEqualTo(0);
        RuleFor(f => f.FieldType).IsInEnum();

        // US-001 AC4: dropdown fields must define their predefined options.
        RuleFor(f => f.Options)
            .Must(options => options is { Count: > 0 })
            .When(f => f.FieldType is FieldType.DropdownSingle or FieldType.DropdownMulti)
            .WithMessage("Dropdown fields must define at least one option.");

        RuleForEach(f => f.Options).ChildRules(option =>
        {
            option.RuleFor(o => o.Label).NotEmpty().MaximumLength(200);
            option.RuleFor(o => o.Value).NotEmpty().MaximumLength(200);
        });

        // US-002 AC2: file upload fields may restrict extensions/size.
        When(f => f.ValidationRule?.MaxFileSizeMb is not null, () =>
        {
            RuleFor(f => f.ValidationRule!.MaxFileSizeMb)
                .GreaterThan(0)
                .WithMessage("Max file size must be greater than 0 MB.");
        });

        When(f => f.ValidationRule?.MinLength is not null && f.ValidationRule?.MaxLength is not null, () =>
        {
            RuleFor(f => f)
                .Must(f => f.ValidationRule!.MinLength <= f.ValidationRule!.MaxLength)
                .WithMessage("Minimum length cannot exceed maximum length.");
        });

        When(f => f.ValidationRule?.MinValue is not null && f.ValidationRule?.MaxValue is not null, () =>
        {
            RuleFor(f => f)
                .Must(f => f.ValidationRule!.MinValue <= f.ValidationRule!.MaxValue)
                .WithMessage("Minimum value cannot exceed maximum value.");
        });
    }
}
