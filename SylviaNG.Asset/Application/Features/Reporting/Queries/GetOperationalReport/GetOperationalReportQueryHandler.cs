using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Reporting.DTOs;
using RMS.Application.Features.Reporting.Services;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Reporting.Queries.GetOperationalReport;

public class GetOperationalReportQueryHandler : IRequestHandler<GetOperationalReportQuery, List<OperationalReportRowDto>>
{
    private readonly IRequisitionRepository _requisitionRepository;
    private readonly ICurrentUserService _currentUser;

    public GetOperationalReportQueryHandler(IRequisitionRepository requisitionRepository, ICurrentUserService currentUser)
    {
        _requisitionRepository = requisitionRepository;
        _currentUser = currentUser;
    }

    public async Task<List<OperationalReportRowDto>> Handle(GetOperationalReportQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsInRole(UserRole.SystemAdmin))
        {
            throw new ForbiddenException();
        }

        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var requisitions = await _requisitionRepository.GetForReportingAsync(
            companyId, request.DateFrom, request.DateTo, request.Department, request.CategoryId, request.CategoryItemId,
            request.Statuses, request.Priority, request.RequesterSearch, cancellationToken);

        return requisitions.Select(ReportingCalculations.ToReportRow).ToList();
    }
}
