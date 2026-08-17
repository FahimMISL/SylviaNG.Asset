using MediatR;
using RMS.Application.Features.Requisitions.DTOs;

namespace RMS.Application.Features.Requisitions.Commands.CancelRequisition;

/// <summary>FR-RR-008: cancel a Submitted requisition before any approver has acted.</summary>
public record CancelRequisitionCommand(Guid Id, string? Comment) : IRequest<RequisitionDto>;
