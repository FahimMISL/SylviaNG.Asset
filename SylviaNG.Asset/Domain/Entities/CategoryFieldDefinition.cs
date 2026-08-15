using RMS.Domain.Enums;

namespace RMS.Domain.Entities;

/// <summary>One configurable form field on a requisition category, per US-001 AC2-4.</summary>
public class CategoryFieldDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public RequisitionCategory? Category { get; set; }

    public string Label { get; set; } = string.Empty;
    public FieldType FieldType { get; set; }
    public bool IsMandatory { get; set; }
    public int DisplayOrder { get; set; }
    public string? HelpText { get; set; }

    public List<CategoryFieldOption> Options { get; set; } = new();
    public CategoryFieldValidationRule? ValidationRule { get; set; }

    public bool IsDropdown => FieldType is FieldType.DropdownSingle or FieldType.DropdownMulti;
}
