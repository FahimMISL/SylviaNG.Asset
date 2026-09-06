using RMS.Domain.Entities;

namespace RMS.Application.Features.RequisitionCategories.DTOs;

public record FieldValidationRuleDto(
    decimal? MinValue,
    decimal? MaxValue,
    int? MinLength,
    int? MaxLength,
    string? RegexPattern,
    string? AllowedFileExtensions,
    int? MaxFileSizeMb)
{
    public static FieldValidationRuleDto FromEntity(CategoryFieldValidationRule r) =>
        new(r.MinValue, r.MaxValue, r.MinLength, r.MaxLength, r.RegexPattern, r.AllowedFileExtensions, r.MaxFileSizeMb);
}
