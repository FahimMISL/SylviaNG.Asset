using RMS.Application.Features.RequisitionCategories.DTOs;
using RMS.Domain.Enums;

namespace RMS.Api.Controllers.Requests;

/// <summary>Same shape as UpdateCategoryCommand minus CategoryId, which is taken from the route.</summary>
public record UpdateCategoryRequestBody(
    string Name,
    string? Description,
    bool IsCostCenterMandatory,
    ProjectCodeRequirement ProjectCodeRequirement,
    List<Guid> CostCenterIds,
    List<FieldDefinitionInput> FieldDefinitions,
    List<CategoryItemInput> Items);
