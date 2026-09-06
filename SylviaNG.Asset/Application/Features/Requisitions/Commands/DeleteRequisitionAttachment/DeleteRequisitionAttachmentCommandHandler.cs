using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Requisitions.Commands.DeleteRequisitionAttachment;

public class DeleteRequisitionAttachmentCommandHandler : IRequestHandler<DeleteRequisitionAttachmentCommand>
{
    private readonly IRequisitionRepository _requisitionRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteRequisitionAttachmentCommandHandler(
        IRequisitionRepository requisitionRepository,
        ICurrentUserService currentUser, IAuditLogger auditLogger, IUnitOfWork unitOfWork)
    {
        _requisitionRepository = requisitionRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteRequisitionAttachmentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();

        var requisition = await _requisitionRepository.GetByIdAsync(request.RequisitionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Requisition), request.RequisitionId);

        if (requisition.RequestedByUserId != userId)
        {
            throw new ForbiddenException();
        }

        // US-007 AC5/AC6: removal only while Draft/Submitted, i.e. before any approver action.
        if (requisition.Status is not (RequisitionStatus.Draft or RequisitionStatus.Submitted))
        {
            throw new ConflictException("Attachments can no longer be removed once an approver has acted on this requisition.");
        }

        var attachment = requisition.Attachments.FirstOrDefault(a => a.Id == request.AttachmentId)
            ?? throw new NotFoundException(nameof(RequisitionAttachment), request.AttachmentId);

        if (attachment.UploadedByUserId != userId)
        {
            throw new ForbiddenException();
        }

        // Feature 13: soft-delete - the row and physical file are both preserved (never destroyed),
        // so this document's audit history stays traceable. It's excluded from GetRequisitionByIdQuery's
        // attachment list and from the active storage-size cap, but nothing is physically removed.
        attachment.IsDeleted = true;
        attachment.DeletedAtUtc = DateTime.UtcNow;
        attachment.DeletedByUserId = userId;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditLogger.LogAsync("RequisitionAttachmentRemoved", nameof(Requisition), requisition.Id, $"File={attachment.FileName}", cancellationToken);
    }
}
