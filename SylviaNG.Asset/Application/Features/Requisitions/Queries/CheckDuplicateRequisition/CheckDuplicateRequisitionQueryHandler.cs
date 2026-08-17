using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Interfaces;

namespace RMS.Application.Features.Requisitions.Queries.CheckDuplicateRequisition;

public class CheckDuplicateRequisitionQueryHandler : IRequestHandler<CheckDuplicateRequisitionQuery, DuplicateCheckResultDto>
{
    private readonly IRequisitionRepository _requisitionRepository;
    private readonly ICurrentUserService _currentUser;

    public CheckDuplicateRequisitionQueryHandler(IRequisitionRepository requisitionRepository, ICurrentUserService currentUser)
    {
        _requisitionRepository = requisitionRepository;
        _currentUser = currentUser;
    }

    public async Task<DuplicateCheckResultDto> Handle(CheckDuplicateRequisitionQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();
        var since = DateTime.UtcNow.AddDays(-7);

        var existing = await _requisitionRepository.FindPotentialDuplicateAsync(
            userId, request.CategoryId, request.NeedByDate, request.TotalQuantity, since, cancellationToken);

        return existing is null
            ? new DuplicateCheckResultDto(false, null, null)
            : new DuplicateCheckResultDto(true, existing.RequisitionNumber, existing.Id);
    }
}
