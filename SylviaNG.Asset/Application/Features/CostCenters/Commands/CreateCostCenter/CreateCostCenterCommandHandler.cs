using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.CostCenters.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.CostCenters.Commands.CreateCostCenter;

public class CreateCostCenterCommandHandler : IRequestHandler<CreateCostCenterCommand, CostCenterDto>
{
    private readonly ICostCenterRepository _costCenterRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCostCenterCommandHandler(
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

    public async Task<CostCenterDto> Handle(CreateCostCenterCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        if (await _costCenterRepository.CodeExistsAsync(companyId, request.Code, null, cancellationToken))
        {
            throw new ConflictException($"A cost center with code '{request.Code}' already exists in your company.");
        }

        var costCenter = new CostCenter
        {
            CompanyId = companyId,
            Code = request.Code,
            Name = request.Name,
            CreatedByUserId = _currentUser.UserId,
            CreatedAtUtc = DateTime.UtcNow,
        };

        _costCenterRepository.Add(costCenter);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync("CostCenterCreated", nameof(CostCenter), costCenter.Id,
            $"Code={costCenter.Code}", cancellationToken);

        return CostCenterDto.FromEntity(costCenter);
    }
}
