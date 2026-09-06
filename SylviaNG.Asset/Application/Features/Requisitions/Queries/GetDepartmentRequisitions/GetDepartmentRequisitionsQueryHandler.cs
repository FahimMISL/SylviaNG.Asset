using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Requisitions.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Requisitions.Queries.GetDepartmentRequisitions;

public class GetDepartmentRequisitionsQueryHandler : IRequestHandler<GetDepartmentRequisitionsQuery, List<RequisitionSummaryDto>>
{
    private readonly IRequisitionRepository _requisitionRepository;
    private readonly ICurrentUserService _currentUser;

    public GetDepartmentRequisitionsQueryHandler(IRequisitionRepository requisitionRepository, ICurrentUserService currentUser)
    {
        _requisitionRepository = requisitionRepository;
        _currentUser = currentUser;
    }

    public async Task<List<RequisitionSummaryDto>> Handle(GetDepartmentRequisitionsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsInRole(UserRole.DepartmentHead))
        {
            throw new ForbiddenException();
        }

        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();
        var department = _currentUser.Department ?? throw new ForbiddenException();

        var requisitions = await _requisitionRepository.GetForDepartmentAsync(companyId, department, cancellationToken);
        return requisitions.Select(RequisitionSummaryDto.FromEntity).ToList();
    }
}
