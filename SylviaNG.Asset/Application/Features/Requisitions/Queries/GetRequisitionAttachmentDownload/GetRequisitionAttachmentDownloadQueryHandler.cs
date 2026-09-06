using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Requisitions.Services;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.Requisitions.Queries.GetRequisitionAttachmentDownload;

public class GetRequisitionAttachmentDownloadQueryHandler : IRequestHandler<GetRequisitionAttachmentDownloadQuery, AttachmentDownloadResult>
{
    private readonly IRequisitionRepository _requisitionRepository;
    private readonly IApprovalDelegationRepository _delegationRepository;
    private readonly IFileStorageService _fileStorage;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;

    public GetRequisitionAttachmentDownloadQueryHandler(
        IRequisitionRepository requisitionRepository, IApprovalDelegationRepository delegationRepository,
        IFileStorageService fileStorage, ICurrentUserService currentUser, IAuditLogger auditLogger)
    {
        _requisitionRepository = requisitionRepository;
        _delegationRepository = delegationRepository;
        _fileStorage = fileStorage;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
    }

    public async Task<AttachmentDownloadResult> Handle(GetRequisitionAttachmentDownloadQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();

        var requisition = await _requisitionRepository.GetByIdAsync(request.RequisitionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Requisition), request.RequisitionId);

        // Feature 10: same access model as GetRequisitionByIdQuery now - previously owner-only, which
        // meant a legitimate approver/procurement/department-head viewer got 403 trying to download an
        // attachment they could already see listed on the detail page.
        var canAccess = await RequisitionAccessHelper.CanAccessAsync(requisition, userId, _currentUser, _delegationRepository, cancellationToken);
        if (!canAccess)
        {
            throw new ForbiddenException();
        }

        // Feature 13: a soft-deleted attachment is hidden from users the same way it's excluded from
        // the requisition detail list - the row/file are preserved for audit purposes, not for
        // re-download through this endpoint.
        var attachment = requisition.Attachments.FirstOrDefault(a => a.Id == request.AttachmentId && !a.IsDeleted)
            ?? throw new NotFoundException(nameof(RequisitionAttachment), request.AttachmentId);

        await _auditLogger.LogAsync(
            "RequisitionAttachmentDownloaded", nameof(Requisition), requisition.Id,
            $"File={attachment.FileName}; Type={attachment.DocumentType}; Version={attachment.Version}", cancellationToken);

        return new AttachmentDownloadResult(_fileStorage.OpenRead(attachment.StoragePath), attachment.FileName, attachment.ContentType);
    }
}
