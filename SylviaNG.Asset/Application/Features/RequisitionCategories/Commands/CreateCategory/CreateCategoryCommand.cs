using MediatR;
using RMS.Application.Features.RequisitionCategories.DTOs;
using RMS.Domain.Enums;

namespace RMS.Application.Features.RequisitionCategories.Commands.CreateCategory;

public record CreateCategoryCommand(
    string Name,
    string? Description,
    bool IsCostCenterMandatory,
    ProjectCodeRequirement ProjectCodeRequirement,
    List<Guid> CostCenterIds,
    List<FieldDefinitionInput> FieldDefinitions,
    List<CategoryItemInput> Items) : IRequest<CategoryDto>;
