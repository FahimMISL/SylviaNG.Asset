using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Procurement.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Procurement.Queries.GetProcurementRequisitions;

public class GetProcurementRequisitionsQueryHandler : IRequestHandler<GetProcurementRequisitionsQuery, List<ProcurementQueueItemDto>>
{
    private static readonly List<RequisitionStatus> PipelineStatuses =
    [
        RequisitionStatus.Approved, RequisitionStatus.PartiallyApproved, RequisitionStatus.InProcurement,
        RequisitionStatus.PartiallyFulfilled, RequisitionStatus.Fulfilled, RequisitionStatus.Closed,
    ];

    private readonly IRequisitionRepository _requisitionRepository;
    private readonly ICurrentUserService _currentUser;

    public GetProcurementRequisitionsQueryHandler(IRequisitionRepository requisitionRepository, ICurrentUserService currentUser)
    {
        _requisitionRepository = requisitionRepository;
        _currentUser = currentUser;
    }

    public async Task<List<ProcurementQueueItemDto>> Handle(GetProcurementRequisitionsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsInRole(UserRole.ProcurementOfficer) && !_currentUser.IsInRole(UserRole.SystemAdmin))
        {
            throw new ForbiddenException();
        }

        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();
        var requisitions = await _requisitionRepository.GetForProcurementAsync(companyId, PipelineStatuses, cancellationToken);
        return requisitions.Select(ProcurementQueueItemDto.FromEntity).ToList();
    }
}
