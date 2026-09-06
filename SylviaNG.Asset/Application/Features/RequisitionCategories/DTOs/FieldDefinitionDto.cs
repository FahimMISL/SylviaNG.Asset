using RMS.Domain.Entities;

namespace RMS.Application.Features.RequisitionCategories.DTOs;

public record FieldDefinitionDto(
    Guid Id,
    string Label,
    string FieldType,
    bool IsMandatory,
    int DisplayOrder,
    string? HelpText,
    List<FieldOptionDto> Options,
    FieldValidationRuleDto? ValidationRule)
{
    public static FieldDefinitionDto FromEntity(CategoryFieldDefinition f) => new(
        f.Id,
        f.Label,
        f.FieldType.ToString(),
        f.IsMandatory,
        f.DisplayOrder,
        f.HelpText,
        f.Options.OrderBy(o => o.DisplayOrder).Select(FieldOptionDto.FromEntity).ToList(),
        f.ValidationRule is null ? null : FieldValidationRuleDto.FromEntity(f.ValidationRule));
}
