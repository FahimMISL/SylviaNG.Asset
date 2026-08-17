using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.CostCenters.DTOs;
using RMS.Application.Interfaces;

namespace RMS.Application.Features.CostCenters.Queries.GetCostCenters;

public class GetCostCentersQueryHandler : IRequestHandler<GetCostCentersQuery, List<CostCenterDto>>
{
    private readonly ICostCenterRepository _costCenterRepository;
    private readonly ICurrentUserService _currentUser;

    public GetCostCentersQueryHandler(ICostCenterRepository costCenterRepository, ICurrentUserService currentUser)
    {
        _costCenterRepository = costCenterRepository;
        _currentUser = currentUser;
    }

    public async Task<List<CostCenterDto>> Handle(GetCostCentersQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();
        var costCenters = await _costCenterRepository.GetAllAsync(companyId, request.IsActive, cancellationToken);
        return costCenters.Select(CostCenterDto.FromEntity).ToList();
    }
}
