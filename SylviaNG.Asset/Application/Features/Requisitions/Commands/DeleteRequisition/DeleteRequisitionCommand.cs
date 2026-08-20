using MediatR;

namespace RMS.Application.Features.Requisitions.Commands.DeleteRequisition;

/// <summary>Permanently deletes a Draft requisition (never a submitted one - use CancelRequisitionCommand
/// for that instead). Only the owning requestor may delete their own Draft.</summary>
public record DeleteRequisitionCommand(Guid Id) : IRequest;
