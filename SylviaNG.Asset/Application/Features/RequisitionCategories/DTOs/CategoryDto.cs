using RMS.Domain.Entities;

namespace RMS.Application.Features.RequisitionCategories.DTOs;

public record CategoryItemDto(Guid Id, string Name, bool IsActive)
{
    public static CategoryItemDto FromEntity(CategoryItem i) => new(i.Id, i.Name, i.IsActive);
}

public record CategoryDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    int CurrentVersionNumber,
    bool IsCostCenterMandatory,
    string ProjectCodeRequirement,
    bool HasNoMandatoryFields,
    Guid? ClonedFromCategoryId,
    List<Guid> LinkedCostCenterIds,
    List<FieldDefinitionDto> FieldDefinitions,
    List<CategoryItemDto> Items)
{
    public static CategoryDto FromEntity(RequisitionCategory c) => new(
        c.Id,
        c.Name,
        c.Description,
        c.IsActive,
        c.CurrentVersionNumber,
        c.IsCostCenterMandatory,
        c.ProjectCodeRequirement.ToString(),
        c.HasNoMandatoryFields,
        c.ClonedFromCategoryId,
        c.CostCenterLinks.Select(l => l.CostCenterId).ToList(),
        c.FieldDefinitions.OrderBy(f => f.DisplayOrder).Select(FieldDefinitionDto.FromEntity).ToList(),
        c.Items.OrderBy(i => i.DisplayOrder).Select(CategoryItemDto.FromEntity).ToList());
}

public record CategorySummaryDto(
    Guid Id,
    string Name,
    bool IsActive,
    int CurrentVersionNumber,
    int FieldCount)
{
    public static CategorySummaryDto FromEntity(RequisitionCategory c) => new(
        c.Id, c.Name, c.IsActive, c.CurrentVersionNumber, c.FieldDefinitions.Count);
}
