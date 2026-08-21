using MediatR;

namespace RMS.Application.Features.Procurement.Commands.CloseProcurement;

public record CloseProcurementCommand(Guid RequisitionId, string? Comment) : IRequest;
