using RMS.Domain.Enums;

namespace RMS.Application.Features.RequisitionCategories.DTOs;

public record FieldOptionInput(string Label, string Value, int DisplayOrder);

public record FieldValidationRuleInput(
    decimal? MinValue,
    decimal? MaxValue,
    int? MinLength,
    int? MaxLength,
    string? RegexPattern,
    string? AllowedFileExtensions,
    int? MaxFileSizeMb);

public record FieldDefinitionInput(
    FieldType FieldType,
    string Label,
    bool IsMandatory,
    int DisplayOrder,
    string? HelpText,
    List<FieldOptionInput>? Options,
    FieldValidationRuleInput? ValidationRule);
