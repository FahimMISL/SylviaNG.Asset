using MediatR;
using RMS.Application.Features.Requisitions.DTOs;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Requisitions.Commands.CreateRequisition;

/// <summary>US-004: submit (or save as draft) a new requisition request.</summary>
public record CreateRequisitionCommand(
    Guid CategoryId,
    List<RequisitionItemInput> Items,
    RequisitionPriority Priority,
    DateTime NeedByDate,
    decimal EstimatedCost,
    string? Justification,
    string? UrgencyJustification,
    Guid? CostCenterId,
    string? ProjectCode,
    List<RequisitionFieldValueInput> FieldValues,
    bool Submit) : IRequest<RequisitionDto>;
