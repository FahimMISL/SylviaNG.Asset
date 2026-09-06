using RMS.Domain.Entities;

namespace RMS.Application.Features.CostCenters.DTOs;

public record CostCenterDto(Guid Id, string Code, string Name, bool IsActive)
{
    public static CostCenterDto FromEntity(CostCenter c) => new(c.Id, c.Code, c.Name, c.IsActive);
}
