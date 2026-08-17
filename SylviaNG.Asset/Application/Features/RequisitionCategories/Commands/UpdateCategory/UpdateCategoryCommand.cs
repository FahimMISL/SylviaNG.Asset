using MediatR;
using RMS.Application.Features.RequisitionCategories.DTOs;
using RMS.Domain.Enums;

namespace RMS.Application.Features.RequisitionCategories.Commands.UpdateCategory;

public record UpdateCategoryCommand(
    Guid CategoryId,
    string Name,
    string? Description,
    bool IsCostCenterMandatory,
    ProjectCodeRequirement ProjectCodeRequirement,
    List<Guid> CostCenterIds,
    List<FieldDefinitionInput> FieldDefinitions,
    List<CategoryItemInput> Items) : IRequest<CategoryDto>;
