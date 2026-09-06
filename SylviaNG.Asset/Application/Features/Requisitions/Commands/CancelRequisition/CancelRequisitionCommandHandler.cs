using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Requisitions.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Requisitions.Commands.CancelRequisition;

public class CancelRequisitionCommandHandler : IRequestHandler<CancelRequisitionCommand, RequisitionDto>
{
    private readonly IRequisitionRepository _requisitionRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public CancelRequisitionCommandHandler(
        IRequisitionRepository requisitionRepository, ICurrentUserService currentUser, IAuditLogger auditLogger, IUnitOfWork unitOfWork)
    {
        _requisitionRepository = requisitionRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task<RequisitionDto> Handle(CancelRequisitionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();
        var actorName = _currentUser.FullName ?? "Unknown";
        var actorRole = _currentUser.Role?.ToString();

        var requisition = await _requisitionRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Requisition), request.Id);

        // BR-RR-003: only the original requestor or a System Admin may cancel.
        var isOwner = requisition.RequestedByUserId == userId;
        var isAdmin = _currentUser.Role == UserRole.SystemAdmin;
        if (!isOwner && !isAdmin)
        {
            throw new ForbiddenException();
        }

        var cancelEntry = requisition.Cancel(userId, actorName, actorRole, request.Comment);
        // requisition was loaded (already tracked), so the new StatusHistory entry needs to be
        // registered explicitly - see Requisition.Submit's remarks.
        _requisitionRepository.AddStatusHistory(cancelEntry);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditLogger.LogAsync("RequisitionCancelled", nameof(Requisition), requisition.Id, request.Comment, cancellationToken);

        var saved = await _requisitionRepository.GetByIdAsync(requisition.Id, cancellationToken) ?? requisition;
        return RequisitionDto.FromEntity(saved);
    }
}
