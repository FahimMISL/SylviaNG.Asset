using MediatR;
using RMS.Application.Features.CostCenters.DTOs;

namespace RMS.Application.Features.CostCenters.Commands.UpdateCostCenter;

public record UpdateCostCenterCommand(Guid CostCenterId, string Code, string Name) : IRequest<CostCenterDto>;
