using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Reporting.DTOs;
using RMS.Application.Features.Reporting.Services;
using RMS.Application.Interfaces;

namespace RMS.Application.Features.Reporting.Queries.GetMyReport;

public class GetMyReportQueryHandler : IRequestHandler<GetMyReportQuery, PersonReportDto>
{
    private readonly IRequisitionRepository _requisitionRepository;
    private readonly ICurrentUserService _currentUser;

    public GetMyReportQueryHandler(IRequisitionRepository requisitionRepository, ICurrentUserService currentUser)
    {
        _requisitionRepository = requisitionRepository;
        _currentUser = currentUser;
    }

    public async Task<PersonReportDto> Handle(GetMyReportQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var requisitions = await _requisitionRepository.GetAllForUserAsync(companyId, userId, cancellationToken);

        return ReportingCalculations.BuildPersonReport(
            userId, _currentUser.FullName ?? "Me", _currentUser.Department, requisitions, request.Period);
    }
}
