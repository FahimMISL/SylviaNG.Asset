using MediatR;

namespace RMS.Application.Features.Requisitions.Queries.GetRequisitionAttachmentDownload;

public record GetRequisitionAttachmentDownloadQuery(Guid RequisitionId, Guid AttachmentId) : IRequest<AttachmentDownloadResult>;

public record AttachmentDownloadResult(Stream Content, string FileName, string ContentType);
