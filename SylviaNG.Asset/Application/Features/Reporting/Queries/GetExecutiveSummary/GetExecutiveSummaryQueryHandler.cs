using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Reporting.DTOs;
using RMS.Application.Features.Reporting.Services;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Reporting.Queries.GetExecutiveSummary;

public class GetExecutiveSummaryQueryHandler : IRequestHandler<GetExecutiveSummaryQuery, ExecutiveSummaryDto>
{
    private readonly IRequisitionRepository _requisitionRepository;
    private readonly ICurrentUserService _currentUser;

    public GetExecutiveSummaryQueryHandler(IRequisitionRepository requisitionRepository, ICurrentUserService currentUser)
    {
        _requisitionRepository = requisitionRepository;
        _currentUser = currentUser;
    }

    public async Task<ExecutiveSummaryDto> Handle(GetExecutiveSummaryQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsInRole(UserRole.SystemAdmin) && !_currentUser.IsInRole(UserRole.Ceo))
        {
            throw new ForbiddenException();
        }

        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var requisitions = await _requisitionRepository.GetForReportingAsync(
            companyId, null, null, null, null, null, null, null, null, cancellationToken);

        return ReportingCalculations.BuildSummary(requisitions);
    }
}
