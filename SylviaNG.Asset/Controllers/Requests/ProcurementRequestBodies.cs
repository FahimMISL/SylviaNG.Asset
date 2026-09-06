using RMS.Application.Features.Procurement.Commands.RecordFulfillment;

namespace RMS.Api.Controllers.Requests;

public record ProcurementCommentRequestBody(string? Comment);

public record RecordFulfillmentRequestBody(List<FulfillmentLineItemInput> Items, string? Comment);
