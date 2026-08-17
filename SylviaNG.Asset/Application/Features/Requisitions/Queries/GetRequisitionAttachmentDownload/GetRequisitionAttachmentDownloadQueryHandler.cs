using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.Requisitions.Queries.GetRequisitionAttachmentDownload;

public class GetRequisitionAttachmentDownloadQueryHandler : IRequestHandler<GetRequisitionAttachmentDownloadQuery, AttachmentDownloadResult>
{
    private readonly IRequisitionRepository _requisitionRepository;
    private readonly IFileStorageService _fileStorage;
    private readonly ICurrentUserService _currentUser;

    public GetRequisitionAttachmentDownloadQueryHandler(
        IRequisitionRepository requisitionRepository, IFileStorageService fileStorage, ICurrentUserService currentUser)
    {
        _requisitionRepository = requisitionRepository;
        _fileStorage = fileStorage;
        _currentUser = currentUser;
    }

    public async Task<AttachmentDownloadResult> Handle(GetRequisitionAttachmentDownloadQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();

        var requisition = await _requisitionRepository.GetByIdAsync(request.RequisitionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Requisition), request.RequisitionId);

        // Same minimal ownership rule as GetRequisitionByIdQuery - approver/admin visibility
        // belongs to later features.
        if (requisition.RequestedByUserId != userId)
        {
            throw new ForbiddenException();
        }

        var attachment = requisition.Attachments.FirstOrDefault(a => a.Id == request.AttachmentId)
            ?? throw new NotFoundException(nameof(RequisitionAttachment), request.AttachmentId);

        return new AttachmentDownloadResult(_fileStorage.OpenRead(attachment.StoragePath), attachment.FileName, attachment.ContentType);
    }
}
