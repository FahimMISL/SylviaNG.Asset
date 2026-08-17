using MediatR;
using RMS.Application.Features.CostCenters.DTOs;

namespace RMS.Application.Features.CostCenters.Commands.CreateCostCenter;

/// <summary>US-003 AC4: cost center master data admins validate requisition cost centers against.</summary>
public record CreateCostCenterCommand(string Code, string Name) : IRequest<CostCenterDto>;
