using RMS.Domain.Common;

namespace RMS.Domain.Entities;

/// <summary>
/// The value an employee entered for one of the category's admin-configured
/// custom fields (US-002), captured on the requisition that used it.
/// </summary>
public class RequisitionFieldValue : AuditableEntity
{
    public Guid RequisitionId { get; set; }
    public Requisition? Requisition { get; set; }

    public Guid FieldDefinitionId { get; set; }
    public CategoryFieldDefinition? FieldDefinition { get; set; }

    /// <summary>Raw entered value; multi-select dropdown values are comma-separated.</summary>
    public string? Value { get; set; }
}
