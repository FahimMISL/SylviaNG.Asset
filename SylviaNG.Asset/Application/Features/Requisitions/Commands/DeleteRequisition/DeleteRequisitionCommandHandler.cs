using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Requisitions.Commands.DeleteRequisition;

/// <summary>Draft or UnderReview are eligible for real deletion, per explicit product decision - the
/// requestor can pull back a request that's actively being reviewed, not just one that was never
/// submitted. Cascades away the whole RequisitionApprovalProcess/RequisitionApproval/Assignments/Actions
/// chain along with it (all Cascade-configured from Requisition down), so this is a genuine hard delete -
/// no audit trail of the approval activity survives, unlike Cancel which keeps the full history. Any
/// other status (Submitted/SentBack/Approved/etc.) must go through CancelRequisitionCommand instead.</summary>
public class DeleteRequisitionCommandHandler : IRequestHandler<DeleteRequisitionCommand>
{
    private readonly IRequisitionRepository _requisitionRepository;
    private readonly IFileStorageService _fileStorage;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteRequisitionCommandHandler(
        IRequisitionRepository requisitionRepository, IFileStorageService fileStorage,
        ICurrentUserService currentUser, IAuditLogger auditLogger, IUnitOfWork unitOfWork)
    {
        _requisitionRepository = requisitionRepository;
        _fileStorage = fileStorage;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteRequisitionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();

        var requisition = await _requisitionRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Requisition), request.Id);

        // Scoped strictly to the owning requestor - never anyone else's draft, not even a System Admin's
        // (an admin acting on someone else's requisition has no real deletion use case here; Cancel
        // already covers the cross-user administrative path once something is Submitted).
        if (requisition.RequestedByUserId != userId)
        {
            throw new ForbiddenException();
        }

        if (requisition.Status != RequisitionStatus.Draft && requisition.Status != RequisitionStatus.UnderReview)
        {
            throw new ConflictException("Only draft or under-review requisitions can be deleted. Use Cancel for anything further along.");
        }

        // Any uploaded files need to go too - mirrors DeleteRequisitionAttachmentCommandHandler,
        // just for every attachment on the requisition instead of a single one.
        foreach (var attachment in requisition.Attachments)
        {
            _fileStorage.Delete(attachment.StoragePath);
        }

        _requisitionRepository.Remove(requisition);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditLogger.LogAsync("RequisitionDeleted", nameof(Requisition), requisition.Id, cancellationToken: cancellationToken);
    }
}
