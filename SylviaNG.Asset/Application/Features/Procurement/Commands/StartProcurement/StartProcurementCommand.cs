using MediatR;

namespace RMS.Application.Features.Procurement.Commands.StartProcurement;

public record StartProcurementCommand(Guid RequisitionId, string? Comment) : IRequest;
