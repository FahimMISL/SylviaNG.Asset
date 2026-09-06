using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.CostCenters.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.CostCenters.Commands.UpdateCostCenter;

public class UpdateCostCenterCommandHandler : IRequestHandler<UpdateCostCenterCommand, CostCenterDto>
{
    private readonly ICostCenterRepository _costCenterRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCostCenterCommandHandler(
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

    public async Task<CostCenterDto> Handle(UpdateCostCenterCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var costCenter = await _costCenterRepository.GetByIdAsync(request.CostCenterId, cancellationToken)
            ?? throw new NotFoundException(nameof(CostCenter), request.CostCenterId);

        if (costCenter.CompanyId != companyId)
        {
            throw new ForbiddenException();
        }

        if (await _costCenterRepository.CodeExistsAsync(companyId, request.Code, costCenter.Id, cancellationToken))
        {
            throw new ConflictException($"A cost center with code '{request.Code}' already exists in your company.");
        }

        costCenter.Code = request.Code;
        costCenter.Name = request.Name;
        costCenter.UpdatedByUserId = _currentUser.UserId;
        costCenter.UpdatedAtUtc = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync("CostCenterUpdated", nameof(CostCenter), costCenter.Id,
            $"Code={costCenter.Code}", cancellationToken);

        return CostCenterDto.FromEntity(costCenter);
    }
}
