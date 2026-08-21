using MediatR;
using Microsoft.EntityFrameworkCore;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Procurement.Services;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Procurement.Commands.StartProcurement;

public class StartProcurementCommandHandler : IRequestHandler<StartProcurementCommand>
{
    private readonly IRequisitionRepository _requisitionRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ProcurementService _service;

    public StartProcurementCommandHandler(
        IRequisitionRepository requisitionRepository, ICurrentUserService currentUser,
        IAuditLogger auditLogger, IUnitOfWork unitOfWork, ProcurementService service)
    {
        _requisitionRepository = requisitionRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
        _service = service;
    }

    public async Task Handle(StartProcurementCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();
        if (!_currentUser.IsInRole(UserRole.ProcurementOfficer) && !_currentUser.IsInRole(UserRole.SystemAdmin))
        {
            throw new ForbiddenException();
        }

        var requisition = await _requisitionRepository.GetByIdAsync(request.RequisitionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Requisition), request.RequisitionId);

        _service.StartProcessing(requisition, userId, _currentUser.FullName ?? "Unknown", _currentUser.Role?.ToString(), request.Comment);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("This requisition was already updated by someone else. Please refresh.");
        }

        await _auditLogger.LogAsync("ProcurementStarted", nameof(Requisition), requisition.Id, cancellationToken: cancellationToken);
    }
}
