using RMS.Domain.Common;
using RMS.Domain.Enums;

namespace RMS.Domain.Entities;

/// <summary>
/// A requisition category template, per US-001. Owns its field definitions
/// and enforces the invariants that must always hold regardless of which
/// application-layer command touched it.
/// </summary>
public class RequisitionCategory : AuditableEntity
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int CurrentVersionNumber { get; set; }
    public bool IsCostCenterMandatory { get; set; }
    public ProjectCodeRequirement ProjectCodeRequirement { get; set; } = ProjectCodeRequirement.NotApplicable;
    public Guid? ClonedFromCategoryId { get; set; }

    public List<CategoryFieldDefinition> FieldDefinitions { get; set; } = new();
    public List<CategoryCostCenterLink> CostCenterLinks { get; set; } = new();
    public List<CategoryTemplateVersion> Versions { get; set; } = new();
    public List<CategoryItem> Items { get; set; } = new();

    /// <summary>US-001 edge case: category with no mandatory fields is a soft warning, not a block.</summary>
    public bool HasNoMandatoryFields => FieldDefinitions.Count == 0 || FieldDefinitions.All(f => !f.IsMandatory);

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    /// <summary>US-001 edge case: dropdown fields must have at least one option before the template can be published.</summary>
    public void EnsurePublishable()
    {
        var dropdownWithoutOptions = FieldDefinitions.FirstOrDefault(f => f.IsDropdown && f.Options.Count == 0);
        if (dropdownWithoutOptions is not null)
        {
            throw new DomainException(
                $"Field '{dropdownWithoutOptions.Label}' is a dropdown but has no options configured.");
        }
    }

    /// <summary>Bumps the version counter; caller (Application layer) persists the CategoryTemplateVersion snapshot.</summary>
    public int IncrementVersion()
    {
        CurrentVersionNumber++;
        return CurrentVersionNumber;
    }

    /// <summary>US-001 AC7: clone an existing category, including its field schema, as a new draft category.</summary>
    public RequisitionCategory CloneAsNew(string newName)
    {
        var clone = new RequisitionCategory
        {
            CompanyId = CompanyId,
            Name = newName,
            Description = Description,
            IsActive = false,
            IsCostCenterMandatory = IsCostCenterMandatory,
            ProjectCodeRequirement = ProjectCodeRequirement,
            ClonedFromCategoryId = Id,
        };

        foreach (var field in FieldDefinitions.OrderBy(f => f.DisplayOrder))
        {
            var clonedField = new CategoryFieldDefinition
            {
                CategoryId = clone.Id,
                Label = field.Label,
                FieldType = field.FieldType,
                IsMandatory = field.IsMandatory,
                DisplayOrder = field.DisplayOrder,
                HelpText = field.HelpText,
            };

            foreach (var option in field.Options.OrderBy(o => o.DisplayOrder))
            {
                clonedField.Options.Add(new CategoryFieldOption
                {
                    FieldDefinitionId = clonedField.Id,
                    Label = option.Label,
                    Value = option.Value,
                    DisplayOrder = option.DisplayOrder,
                });
            }

            if (field.ValidationRule is not null)
            {
                clonedField.ValidationRule = new CategoryFieldValidationRule
                {
                    FieldDefinitionId = clonedField.Id,
                    MinValue = field.ValidationRule.MinValue,
                    MaxValue = field.ValidationRule.MaxValue,
                    MinLength = field.ValidationRule.MinLength,
                    MaxLength = field.ValidationRule.MaxLength,
                    RegexPattern = field.ValidationRule.RegexPattern,
                    AllowedFileExtensions = field.ValidationRule.AllowedFileExtensions,
                    MaxFileSizeMb = field.ValidationRule.MaxFileSizeMb,
                };
            }

            clone.FieldDefinitions.Add(clonedField);
        }

        return clone;
    }
}
