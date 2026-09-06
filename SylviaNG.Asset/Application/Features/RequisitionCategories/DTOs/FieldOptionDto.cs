using RMS.Domain.Entities;

namespace RMS.Application.Features.RequisitionCategories.DTOs;

public record FieldOptionDto(Guid Id, string Label, string Value, int DisplayOrder)
{
    public static FieldOptionDto FromEntity(CategoryFieldOption o) => new(o.Id, o.Label, o.Value, o.DisplayOrder);
}
