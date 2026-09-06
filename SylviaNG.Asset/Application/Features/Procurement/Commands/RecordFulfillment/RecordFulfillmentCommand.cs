using MediatR;

namespace RMS.Application.Features.Procurement.Commands.RecordFulfillment;

public record FulfillmentLineItemInput(Guid RequisitionItemId, int Quantity);

public record RecordFulfillmentCommand(Guid RequisitionId, List<FulfillmentLineItemInput> Items, string? Comment) : IRequest;
