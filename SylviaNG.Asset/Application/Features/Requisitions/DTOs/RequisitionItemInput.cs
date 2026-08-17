namespace RMS.Application.Features.Requisitions.DTOs;

/// <summary>US-004/scope-expansion: Item must be one of the category's Admin-defined Items/Types,
/// selected by id - never free text, so the backend always resolves and validates the name itself.</summary>
public record RequisitionItemInput(Guid CategoryItemId, int Quantity);

/// <summary>US-002: the employee's entered value for one of the category's admin-configured custom fields.</summary>
public record RequisitionFieldValueInput(Guid FieldDefinitionId, string? Value);
