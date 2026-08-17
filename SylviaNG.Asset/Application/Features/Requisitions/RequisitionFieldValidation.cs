using System.Text.RegularExpressions;
using FluentValidation;
using FluentValidation.Results;
using RMS.Application.Features.Requisitions.DTOs;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Requisitions;

/// <summary>
/// Validates an employee's dynamic field values, cost center, and project code
/// against the rules the Admin configured on the category (US-002, US-003).
/// Shared between Create/Update handlers since both need the same checks.
/// </summary>
public static class RequisitionFieldValidation
{
    public static void EnsureValid(
        RequisitionCategory category,
        List<RequisitionFieldValueInput> fieldValues,
        Guid? costCenterId,
        string? projectCode,
        bool submit)
    {
        var failures = new List<ValidationFailure>();
        var valuesByField = fieldValues
            .Where(v => v.Value is not null)
            .ToDictionary(v => v.FieldDefinitionId, v => v.Value);

        foreach (var field in category.FieldDefinitions)
        {
            valuesByField.TryGetValue(field.Id, out var value);
            var isBlank = string.IsNullOrWhiteSpace(value);

            if (submit && field.IsMandatory && isBlank)
            {
                failures.Add(new ValidationFailure(field.Label, $"'{field.Label}' is required."));
                continue;
            }

            if (isBlank || field.ValidationRule is null)
            {
                continue;
            }

            var rule = field.ValidationRule;

            if (field.FieldType is FieldType.Text or FieldType.TextArea)
            {
                if (rule.MinLength.HasValue && value!.Length < rule.MinLength.Value)
                {
                    failures.Add(new ValidationFailure(field.Label, $"'{field.Label}' must be at least {rule.MinLength} characters."));
                }
                if (rule.MaxLength.HasValue && value!.Length > rule.MaxLength.Value)
                {
                    failures.Add(new ValidationFailure(field.Label, $"'{field.Label}' must be at most {rule.MaxLength} characters."));
                }
                if (!string.IsNullOrEmpty(rule.RegexPattern) && !Regex.IsMatch(value!, rule.RegexPattern))
                {
                    failures.Add(new ValidationFailure(field.Label, $"'{field.Label}' is not in the expected format."));
                }
            }
            else if (field.FieldType == FieldType.Number)
            {
                if (decimal.TryParse(value, out var numeric))
                {
                    if (rule.MinValue.HasValue && numeric < rule.MinValue.Value)
                    {
                        failures.Add(new ValidationFailure(field.Label, $"'{field.Label}' must be at least {rule.MinValue}."));
                    }
                    if (rule.MaxValue.HasValue && numeric > rule.MaxValue.Value)
                    {
                        failures.Add(new ValidationFailure(field.Label, $"'{field.Label}' must be at most {rule.MaxValue}."));
                    }
                }
                else
                {
                    failures.Add(new ValidationFailure(field.Label, $"'{field.Label}' must be a number."));
                }
            }
        }

        if (submit && category.IsCostCenterMandatory && costCenterId is null)
        {
            failures.Add(new ValidationFailure("CostCenter", "Cost Center is required for this category."));
        }

        if (costCenterId.HasValue && category.CostCenterLinks.All(l => l.CostCenterId != costCenterId.Value))
        {
            failures.Add(new ValidationFailure("CostCenter", "The selected cost center is not linked to this category."));
        }

        if (submit && category.ProjectCodeRequirement == ProjectCodeRequirement.Mandatory && string.IsNullOrWhiteSpace(projectCode))
        {
            failures.Add(new ValidationFailure("ProjectCode", "Project Code is required for this category."));
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }
    }

    /// <summary>
    /// Resolves each submitted item against the category's Admin-defined Items/Types and returns
    /// real RequisitionItem entities with the server's own Name - the client never supplies free
    /// text, so there is no way to bypass the dropdown through the API.
    /// </summary>
    public static List<RequisitionItem> ResolveItems(RequisitionCategory category, List<RequisitionItemInput> items, bool submit)
    {
        var failures = new List<ValidationFailure>();
        var resolved = new List<RequisitionItem>();

        foreach (var input in items)
        {
            var categoryItem = category.Items.FirstOrDefault(i => i.Id == input.CategoryItemId);
            if (categoryItem is null || !categoryItem.IsActive)
            {
                failures.Add(new ValidationFailure("Items", "One of the selected Items/Types is not available for this category."));
                continue;
            }

            resolved.Add(new RequisitionItem
            {
                CategoryItemId = categoryItem.Id,
                ItemName = categoryItem.Name,
                Quantity = input.Quantity,
            });
        }

        if (submit && resolved.Count == 0)
        {
            failures.Add(new ValidationFailure("Items", "Add at least one item before submitting."));
        }
        if (submit && resolved.Any(r => r.Quantity <= 0))
        {
            failures.Add(new ValidationFailure("Items", "Quantity must be greater than zero."));
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return resolved;
    }
}
