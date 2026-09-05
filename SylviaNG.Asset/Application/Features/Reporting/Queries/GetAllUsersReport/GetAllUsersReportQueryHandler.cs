using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Reporting.DTOs;
using RMS.Application.Features.Reporting.Services;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Reporting.Queries.GetAllUsersReport;

public class GetAllUsersReportQueryHandler : IRequestHandler<GetAllUsersReportQuery, AllUsersReportDto>
{
    private readonly IRequisitionRepository _requisitionRepository;
    private readonly ICurrentUserService _currentUser;

    public GetAllUsersReportQueryHandler(IRequisitionRepository requisitionRepository, ICurrentUserService currentUser)
    {
        _requisitionRepository = requisitionRepository;
        _currentUser = currentUser;
    }

    public async Task<AllUsersReportDto> Handle(GetAllUsersReportQuery request, CancellationToken cancellationToken)
    {
        var isDepartmentHead = _currentUser.IsInRole(UserRole.DepartmentHead);
        if (!_currentUser.IsInRole(UserRole.SystemAdmin) && !_currentUser.IsInRole(UserRole.Ceo) && !isDepartmentHead)
        {
            throw new ForbiddenException();
        }

        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();
        string? department = null;
        if (isDepartmentHead)
        {
            department = _currentUser.Department ?? throw new ForbiddenException();
        }

        var requisitions = await _requisitionRepository.GetForReportingAsync(
            companyId, null, null, department, null, null, null, null, null, cancellationToken);

        var people = requisitions
            .Where(r => r.RequestedByUser is not null)
            .GroupBy(r => r.RequestedByUser!)
            .Select(g => ReportingCalculations.BuildPersonReport(g.Key.Id, g.Key.FullName, g.Key.Department, g.ToList(), request.Period))
            .OrderByDescending(p => p.TotalCount)
            .ToList();

        var overall = ReportingCalculations.BuildPersonReport(null, "All Users", department, requisitions, request.Period);

        return new AllUsersReportDto(overall, people);
    }
}
