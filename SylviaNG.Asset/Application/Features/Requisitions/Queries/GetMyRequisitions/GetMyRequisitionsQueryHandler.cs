using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Requisitions.DTOs;
using RMS.Application.Interfaces;

namespace RMS.Application.Features.Requisitions.Queries.GetMyRequisitions;

public class GetMyRequisitionsQueryHandler : IRequestHandler<GetMyRequisitionsQuery, List<RequisitionSummaryDto>>
{
    private readonly IRequisitionRepository _requisitionRepository;
    private readonly ICurrentUserService _currentUser;

    public GetMyRequisitionsQueryHandler(IRequisitionRepository requisitionRepository, ICurrentUserService currentUser)
    {
        _requisitionRepository = requisitionRepository;
        _currentUser = currentUser;
    }

    public async Task<List<RequisitionSummaryDto>> Handle(GetMyRequisitionsQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();
        var userId = _currentUser.UserId ?? throw new ForbiddenException();

        var requisitions = await _requisitionRepository.GetAllForUserAsync(companyId, userId, cancellationToken);
        return requisitions.Select(RequisitionSummaryDto.FromEntity).ToList();
    }
}
