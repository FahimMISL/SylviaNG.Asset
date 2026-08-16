namespace RMS.Domain.Entities;

/// <summary>Predefined dropdown option per US-001 AC4.</summary>
public class CategoryFieldOption
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FieldDefinitionId { get; set; }
    public CategoryFieldDefinition? FieldDefinition { get; set; }

    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
