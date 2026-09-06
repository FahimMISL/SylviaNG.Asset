using MediatR;

namespace RMS.Application.Features.Requisitions.Commands.DeleteRequisitionAttachment;

public record DeleteRequisitionAttachmentCommand(Guid RequisitionId, Guid AttachmentId) : IRequest;
