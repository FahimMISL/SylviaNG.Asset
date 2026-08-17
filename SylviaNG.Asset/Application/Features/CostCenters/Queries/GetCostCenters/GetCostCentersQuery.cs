using MediatR;
using RMS.Application.Features.CostCenters.DTOs;

namespace RMS.Application.Features.CostCenters.Queries.GetCostCenters;

public record GetCostCentersQuery(bool? IsActive) : IRequest<List<CostCenterDto>>;
