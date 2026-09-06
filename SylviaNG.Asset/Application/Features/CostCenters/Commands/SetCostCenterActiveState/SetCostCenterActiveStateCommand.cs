using MediatR;
using RMS.Application.Features.CostCenters.DTOs;

namespace RMS.Application.Features.CostCenters.Commands.SetCostCenterActiveState;

public record SetCostCenterActiveStateCommand(Guid CostCenterId, bool IsActive) : IRequest<CostCenterDto>;
