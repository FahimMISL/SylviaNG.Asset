using MediatR;

namespace RMS.Application.Features.CostCenters.Commands.DeleteCostCenter;

public record DeleteCostCenterCommand(Guid Id) : IRequest;
