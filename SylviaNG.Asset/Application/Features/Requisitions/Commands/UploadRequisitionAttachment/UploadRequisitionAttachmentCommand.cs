using MediatR;
using RMS.Application.Features.Requisitions.DTOs;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Requisitions.Commands.UploadRequisitionAttachment;

/// <summary>
/// US-007 / Feature 13. Reason is only required once the requisition is past Draft/Submitted (i.e.
/// an approver has already acted) - the acceptance criteria allow addition after that point, but
/// only with a logged reason; removal is never allowed past that point (see
/// DeleteRequisitionAttachmentCommand). DocumentType defaults to SupportingDocument so the original,
/// simple upload flow keeps working unchanged for callers that don't pick a type. The server computes
/// Version itself from existing rows in the same (RequisitionId, DocumentType) lineage - never
/// caller-supplied, so it can't be spoofed or skipped.
/// </summary>
public record UploadRequisitionAttachmentCommand(
    Guid RequisitionId, string FileName, string ContentType, long SizeBytes, Stream Content, string? Reason,
    DocumentType DocumentType = DocumentType.SupportingDocument)
    : IRequest<RequisitionAttachmentDto>;
