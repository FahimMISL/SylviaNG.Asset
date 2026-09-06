using RMS.Application.Features.Requisitions.DTOs;
using RMS.Domain.Enums;

namespace RMS.Api.Controllers.Requests;

public record SaveRequisitionRequestBody(
    Guid CategoryId,
    List<RequisitionItemInput> Items,
    RequisitionPriority Priority,
    DateTime? NeedByDate,
    decimal EstimatedCost,
    string? Justification,
    string? UrgencyJustification,
    Guid? CostCenterId,
    string? ProjectCode,
    List<RequisitionFieldValueInput> FieldValues,
    string? ResubmitComment,
    bool Submit);

public record CancelRequisitionRequestBody(string? Comment);
