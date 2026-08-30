using MediatR;
using Microsoft.EntityFrameworkCore;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Procurement.Services;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Procurement.Commands.RecordFulfillment;

public class RecordFulfillmentCommandHandler : IRequestHandler<RecordFulfillmentCommand>
{
    private readonly IRequisitionRepository _requisitionRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ProcurementService _service;
    private readonly INotificationService _notificationService;

    public RecordFulfillmentCommandHandler(
        IRequisitionRepository requisitionRepository, ICurrentUserService currentUser,
        IAuditLogger auditLogger, IUnitOfWork unitOfWork, ProcurementService service, INotificationService notificationService)
    {
        _requisitionRepository = requisitionRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
        _service = service;
        _notificationService = notificationService;
    }

    public async Task Handle(RecordFulfillmentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();
        if (!_currentUser.IsInRole(UserRole.ProcurementOfficer) && !_currentUser.IsInRole(UserRole.SystemAdmin))
        {
            throw new ForbiddenException();
        }

        var requisition = await _requisitionRepository.GetByIdAsync(request.RequisitionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Requisition), request.RequisitionId);

        _service.RecordFulfillment(
            requisition, userId, _currentUser.FullName ?? "Unknown", _currentUser.Role?.ToString(), request.Comment,
            request.Items.Select(i => (i.RequisitionItemId, i.Quantity)).ToList());

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("This requisition was already updated by someone else. Please refresh.");
        }

        // Feature 8: item names + quantities recorded this action, so the audit trail shows exactly
        // what was fulfilled, not just that "something" was.
        var itemNames = requisition.Items.ToDictionary(i => i.Id, i => i.ItemName);
        var fulfilledSummary = string.Join(", ", request.Items
            .Where(i => i.Quantity > 0)
            .Select(i => $"{itemNames.GetValueOrDefault(i.RequisitionItemId, "Unknown item")} x{i.Quantity}"));

        await _auditLogger.LogAsync(
            "ProcurementFulfillmentRecorded", nameof(Requisition), requisition.Id,
            $"Fulfilled: {fulfilledSummary}; Comment={request.Comment}", cancellationToken);

        // Feature 9 (US-030): Fulfilled is critical (see NotificationEventTypeCatalog); Partially
        // Fulfilled isn't - requisition.Status was already mutated in-memory by RecordFulfillment above
        // (Fulfill()/PartiallyFulfill()), so it's the authoritative signal for which one just happened.
        var eventType = requisition.Status == RequisitionStatus.Fulfilled
            ? NotificationEventType.RequisitionFulfilled
            : NotificationEventType.RequisitionPartiallyFulfilled;
        await _notificationService.NotifyAsync(new NotificationRequest(
            requisition.CompanyId, requisition.RequestedByUserId, eventType, requisition.Id,
            new Dictionary<string, string>
            {
                ["RequisitionNumber"] = requisition.RequisitionNumber ?? "N/A",
                ["Comment"] = fulfilledSummary,
            }), cancellationToken);
    }
}
