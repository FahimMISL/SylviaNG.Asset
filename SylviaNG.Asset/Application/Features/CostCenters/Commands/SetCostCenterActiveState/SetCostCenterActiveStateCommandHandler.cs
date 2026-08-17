using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.CostCenters.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.CostCenters.Commands.SetCostCenterActiveState;

public class SetCostCenterActiveStateCommandHandler : IRequestHandler<SetCostCenterActiveStateCommand, CostCenterDto>
{
    private readonly ICostCenterRepository _costCenterRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public SetCostCenterActiveStateCommandHandler(
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

    public async Task<CostCenterDto> Handle(SetCostCenterActiveStateCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var costCenter = await _costCenterRepository.GetByIdAsync(request.CostCenterId, cancellationToken)
            ?? throw new NotFoundException(nameof(CostCenter), request.CostCenterId);

        if (costCenter.CompanyId != companyId)
        {
            throw new ForbiddenException();
        }

        costCenter.IsActive = request.IsActive;
        costCenter.UpdatedByUserId = _currentUser.UserId;
        costCenter.UpdatedAtUtc = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            request.IsActive ? "CostCenterActivated" : "CostCenterDeactivated",
            nameof(CostCenter), costCenter.Id, null, cancellationToken);

        return CostCenterDto.FromEntity(costCenter);
    }
}
