using RMS.Application.Features.RequisitionCategories.DTOs;
using RMS.Domain.Entities;

namespace RMS.Application.Features.RequisitionCategories.Mappings;

/// <summary>Builds domain field-definition entities from the request-shaped inputs. Shared by Create and Update handlers.</summary>
public static class CategoryFieldMapper
{
    public static List<CategoryFieldDefinition> ToEntities(Guid categoryId, IEnumerable<FieldDefinitionInput> inputs)
    {
        var result = new List<CategoryFieldDefinition>();

        foreach (var input in inputs)
        {
            var field = new CategoryFieldDefinition
            {
                CategoryId = categoryId,
                Label = input.Label,
                FieldType = input.FieldType,
                IsMandatory = input.IsMandatory,
                DisplayOrder = input.DisplayOrder,
                HelpText = input.HelpText,
            };

            if (input.Options is not null)
            {
                foreach (var option in input.Options)
                {
                    field.Options.Add(new CategoryFieldOption
                    {
                        FieldDefinitionId = field.Id,
                        Label = option.Label,
                        Value = option.Value,
                        DisplayOrder = option.DisplayOrder,
                    });
                }
            }

            if (input.ValidationRule is not null)
            {
                var rule = input.ValidationRule;
                field.ValidationRule = new CategoryFieldValidationRule
                {
                    FieldDefinitionId = field.Id,
                    MinValue = rule.MinValue,
                    MaxValue = rule.MaxValue,
                    MinLength = rule.MinLength,
                    MaxLength = rule.MaxLength,
                    RegexPattern = rule.RegexPattern,
                    AllowedFileExtensions = rule.AllowedFileExtensions,
                    MaxFileSizeMb = rule.MaxFileSizeMb,
                };
            }

            result.Add(field);
        }

        return result;
    }
}
