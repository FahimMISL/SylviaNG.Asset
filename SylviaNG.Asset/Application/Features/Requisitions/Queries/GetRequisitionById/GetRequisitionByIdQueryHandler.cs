using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Requisitions.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.Requisitions.Queries.GetRequisitionById;

public class GetRequisitionByIdQueryHandler : IRequestHandler<GetRequisitionByIdQuery, RequisitionDto>
{
    private readonly IRequisitionRepository _requisitionRepository;
    private readonly ICurrentUserService _currentUser;

    public GetRequisitionByIdQueryHandler(IRequisitionRepository requisitionRepository, ICurrentUserService currentUser)
    {
        _requisitionRepository = requisitionRepository;
        _currentUser = currentUser;
    }

    public async Task<RequisitionDto> Handle(GetRequisitionByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();

        var requisition = await _requisitionRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Requisition), request.Id);

        // Minimal ownership rule for this slice: a requester can only view their own
        // requisition. Approver/admin visibility belongs to later features.
        if (requisition.RequestedByUserId != userId)
        {
            throw new ForbiddenException();
        }

        return RequisitionDto.FromEntity(requisition);
    }
}
