using MediatR;
using RMS.Application.Features.Requisitions.DTOs;

namespace RMS.Application.Features.Requisitions.Commands.UploadRequisitionAttachment;

/// <summary>
/// US-007. Reason is only required once the requisition is past Draft/Submitted (i.e. an approver
/// has already acted) - the acceptance criteria allow addition after that point, but only with a
/// logged reason; removal is never allowed past that point (see DeleteRequisitionAttachmentCommand).
/// </summary>
public record UploadRequisitionAttachmentCommand(
    Guid RequisitionId, string FileName, string ContentType, long SizeBytes, Stream Content, string? Reason)
    : IRequest<RequisitionAttachmentDto>;
