using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Dashboard.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Dashboard.Queries.GetManpowerSummary;

public class GetManpowerSummaryQueryHandler : IRequestHandler<GetManpowerSummaryQuery, ManpowerSummaryDto>
{
    private readonly IRequisitionRepository _requisitionRepository;
    private readonly ICurrentUserService _currentUser;

    public GetManpowerSummaryQueryHandler(IRequisitionRepository requisitionRepository, ICurrentUserService currentUser)
    {
        _requisitionRepository = requisitionRepository;
        _currentUser = currentUser;
    }

    public async Task<ManpowerSummaryDto> Handle(GetManpowerSummaryQuery request, CancellationToken cancellationToken)
    {
        // Manpower/View alone isn't precise enough here - DepartmentHead also holds that grant (for
        // their own department's requisitions) but must not get this company-wide read.
        if (!_currentUser.IsInRole(UserRole.HrManager) && !_currentUser.IsInRole(UserRole.SystemAdmin))
        {
            throw new ForbiddenException();
        }

        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();
        var requisitions = await _requisitionRepository.GetManpowerForCompanyAsync(companyId, cancellationToken);

        var totalPositionsRequested = requisitions.SelectMany(r => r.Items).Sum(i => i.Quantity);

        var topPositions = requisitions
            .SelectMany(r => r.Items)
            .GroupBy(i => i.ItemName)
            .Select(g => new PositionQuantityDto(g.Key, g.Sum(i => i.Quantity)))
            .OrderByDescending(p => p.Quantity)
            .Take(5)
            .ToList();

        var recentRequisitions = requisitions
            .Take(10)
            .Select(r => new ManpowerRequisitionSummaryDto(
                r.Id, r.RequisitionNumber, r.RequestedByUser?.FullName ?? "(unknown)", r.Status.ToString(), r.CreatedAtUtc,
                r.Items.Sum(i => i.Quantity),
                r.Items.Select(i => new PositionQuantityDto(i.ItemName, i.Quantity)).ToList()))
            .ToList();

        return new ManpowerSummaryDto(recentRequisitions, totalPositionsRequested, topPositions);
    }
}
