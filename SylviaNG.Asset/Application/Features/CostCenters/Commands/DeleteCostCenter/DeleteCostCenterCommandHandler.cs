using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.CostCenters.Commands.DeleteCostCenter;

public class DeleteCostCenterCommandHandler : IRequestHandler<DeleteCostCenterCommand>
{
    private readonly ICostCenterRepository _costCenterRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCostCenterCommandHandler(
        ICostCenterRepository costCenterRepository,
        ICurrentUserService currentUser,
        IAuditLogger auditLogger,
        IUnitOfWork unitOfWork)
    {
        _costCenterRepository = costCenterRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteCostCenterCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var costCenter = await _costCenterRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(CostCenter), request.Id);

        if (costCenter.CompanyId != companyId)
        {
            throw new ForbiddenException();
        }

        if (await _costCenterRepository.IsInUseAsync(request.Id, cancellationToken))
        {
            throw new ConflictException(
                "This cost center is linked to a category or used in a requisition and can't be deleted. Deactivate it instead.");
        }

        _costCenterRepository.Delete(costCenter);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync("CostCenterDeleted", nameof(CostCenter), costCenter.Id, null, cancellationToken);
    }
}
