namespace RMS.Domain.Entities;

/// <summary>Per-field validation rules per US-002 AC1-2. One-to-one with a field definition.</summary>
public class CategoryFieldValidationRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FieldDefinitionId { get; set; }
    public CategoryFieldDefinition? FieldDefinition { get; set; }

    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string? RegexPattern { get; set; }

    /// <summary>Comma-separated extensions, e.g. "pdf,docx,xlsx,jpeg,png". FileUpload fields only.</summary>
    public string? AllowedFileExtensions { get; set; }

    /// <summary>Defaults to 10 MB per US-002 AC2 if not set.</summary>
    public int? MaxFileSizeMb { get; set; }
}
